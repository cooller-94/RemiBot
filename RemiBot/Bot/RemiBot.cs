using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bot;
using Bot.Constants;
using Bot.Dialogs;
using Bot.Models;
using Bot.Services;
using Hangfire;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using RemiBod;
using RemiBot;

namespace Bot_Builder_Echo_Bot_V4
{
    public class RemiBot : IBot
    {
        public const string JobCompleteEventName = "jobComplete";

        public const string WelcomeText = @"Hello. I am reminder bot. You can create a reminder or just left a note.";

        private readonly RemiBotAccessors _accessors;
        private readonly ILogger _logger;

        private readonly JobState _jobState;
        private readonly IStatePropertyAccessor<JobLog> _jobLogPropertyAccessor;
        private readonly EndpointService _endpointService;
        private readonly RemiBotGeneratorService _generatorService;
        private readonly RemiBotService _remiService;

        private ReminderDialogs _dialogs;

        public RemiBot(JobState jobState, EndpointService endpointService, RemiBotAccessors accessors, ILoggerFactory loggerFactory, RemiBotGeneratorService generatorService)
        {
            _accessors = accessors ?? throw new ArgumentNullException(nameof(accessors));
            _dialogs = new ReminderDialogs(accessors.ConversationDialogState, accessors.UserProfileAccessor);
            _jobState = jobState ?? throw new ArgumentNullException(nameof(jobState));
            _jobLogPropertyAccessor = _jobState.CreateProperty<JobLog>(nameof(JobLog));
            _endpointService = endpointService;
            _generatorService = generatorService;
            _logger = loggerFactory.CreateLogger<RemiBot>();
            _remiService = new RemiBotService(accessors, generatorService);

            _logger.LogTrace("RemiBot turn start");
        }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                DialogContext dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);
                JobLog jobLog = await _jobLogPropertyAccessor.GetAsync(turnContext, () => new JobLog());

                string utterance = turnContext.Activity.Text.Trim().ToLowerInvariant();

                if (utterance == "cancel")
                {
                    await OnCancelAsync(turnContext, dialogContext, cancellationToken);
                }
                else if (utterance == "help")
                {
                    string helpMessage = _generatorService.GenerateHelpMessage();
                    await turnContext.SendActivityAsync(helpMessage);
                }
                else
                {
                    DialogTurnResult results = await dialogContext.ContinueDialogAsync(cancellationToken);

                    if (results.Status == DialogTurnStatus.Empty)
                    {
                        if (utterance.Contains(RemiBotCommandConstants.ShowLastCommand))
                        {
                            string message = await _remiService.GetShowLastNotesMessageAsync(turnContext, cancellationToken, utterance);
                            await turnContext.SendActivityAsync(message);
                            return;
                        }

                        switch (utterance)
                        {
                            case RemiBotCommandConstants.NewReminder:
                                await OnNewReminderAsync(turnContext, jobLog);
                                break;
                            case RemiBotCommandConstants.NewNoteCommand:
                                await OnNewNoteAsync(dialogContext, cancellationToken);
                                break;
                            default:
                                break;
                        }
                    }
                }

                if (!turnContext.Responded)
                {
                    Activity reply = CreateSuggestedActivity(turnContext, "What do you want?");

                    await turnContext.SendActivityAsync(reply);
                }
            }
            else
            {
                await OnSystemActivityAsync(turnContext);
            }

            await _accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _accessors.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        public async Task RunJobAsync(JobLog.JobData jobLog)
        {
            await CompleteJobAsync(jobLog);
        }

        private async Task OnCancelAsync(ITurnContext turnContext, DialogContext dialogContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync("Canceled.", cancellationToken: cancellationToken);
            await dialogContext.CancelAllDialogsAsync(cancellationToken);
        }

        private async Task OnNewReminderAsync(ITurnContext turnContext, JobLog jobLog)
        {
            JobLog.JobData job = SetReminder(turnContext, jobLog);

            await _jobLogPropertyAccessor.SetAsync(turnContext, jobLog);
            await _jobState.SaveChangesAsync(turnContext);

            await turnContext.SendActivityAsync("You reminder was scheduled");
        }

        private async Task OnNewNoteAsync(DialogContext dialogContext, CancellationToken cancellationToken)
        {
            await dialogContext.BeginDialogAsync("newNote", null, cancellationToken);
        }

        private async Task SendWelcomeMessageAsync(ITurnContext turnContext)
        {
            foreach (var member in turnContext.Activity.MembersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    string welcomeMessage = $"Welcome to RemiBet {member.Name}.\r\n " +
                        $"{WelcomeText} \r\n" +
                        $"What do you want?";

                    Activity reply = CreateSuggestedActivity(turnContext, welcomeMessage);

                    await turnContext.SendActivityAsync(reply);
                }
            }
        }

        private Activity CreateSuggestedActivity(ITurnContext turnContext, string message)
        {
            Activity reply = turnContext.Activity.CreateReply(message);

            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>
                {
                    new CardAction() { Title = "New note", Type = ActionTypes.ImBack, Value = "new note", Text = "new note" },
                    new CardAction() { Title = "Help", Type = ActionTypes.ImBack, Value = "Help", Text = "Help" },
                },
            };

            return reply;
        }

        private async Task OnSystemActivityAsync(ITurnContext turnContext)
        {
            if (turnContext.Activity.Type is ActivityTypes.ConversationUpdate)
            {
                if (turnContext.Activity.MembersAdded.Any())
                {
                    await SendWelcomeMessageAsync(turnContext);
                }
            }
        }

        private JobLog.JobData SetReminder(ITurnContext turnContext, JobLog jobLog)
        {
            JobLog.JobData jobInfo = new JobLog.JobData
            {
                TimeStamp = DateTime.Now.ToBinary(),
                Conversation = turnContext.Activity.GetConversationReference(),
            };

            jobLog[jobInfo.TimeStamp] = jobInfo;

            BackgroundJob.Schedule(() => RunJobAsync(jobInfo), TimeSpan.FromSeconds(20));

            return jobInfo;
        }

        // Sends a proactive message to the user.
        private async Task CompleteJobAsync(JobLog.JobData jobInfo, CancellationToken cancellationToken = default(CancellationToken))
        {
            IMessageActivity message = Activity.CreateMessageActivity();

            message.ChannelId = jobInfo.Conversation.ChannelId;
            message.From = jobInfo.Conversation.Bot;
            message.Recipient = jobInfo.Conversation.User;
            message.Conversation = jobInfo.Conversation.Conversation;
            message.Text = "You job is completed!!!";
            message.Locale = "en-us";

            var connector = new ConnectorClient(new Uri(jobInfo.Conversation.ServiceUrl), _endpointService.AppId, _endpointService.AppPassword);
            await connector.Conversations.SendToConversationAsync((Activity)message);
        }
    }
}
