using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Bot.Builder;
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

        public const string WelcomeText = @"Hello. Type 'run' to start a new job.";

        private readonly RemiBotAccessors _accessors;
        private readonly ILogger _logger;

        private readonly JobState _jobState;
        private readonly IStatePropertyAccessor<JobLog> _jobLogPropertyAccessor;
        private readonly EndpointService _endpointService;

        public RemiBot(JobState jobState, EndpointService endpointService)
        {
            _jobState = jobState ?? throw new ArgumentNullException(nameof(jobState));
            _jobLogPropertyAccessor = _jobState.CreateProperty<JobLog>(nameof(JobLog));
            _endpointService = endpointService;
        }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                JobLog jobLog = await _jobLogPropertyAccessor.GetAsync(turnContext, () => new JobLog());

                string text = turnContext.Activity.Text.Trim().ToLowerInvariant();

                switch (text)
                {
                    case "run":
                        JobLog.JobData job = SetReminder(turnContext, jobLog);

                        await _jobLogPropertyAccessor.SetAsync(turnContext, jobLog);
                        await _jobState.SaveChangesAsync(turnContext);

                        string message = "You reminder was scheduled";

                        await turnContext.SendActivityAsync(message);

                        break;

                    default:
                        break;
                }

                if (!turnContext.Responded)
                {
                    await turnContext.SendActivityAsync(WelcomeText);
                }
            }
            else
            {
                await OnSystemActivityAsync(turnContext);
            }
        }

        public async Task RunJobAsync(JobLog.JobData jobLog)
        {
            await CompleteJobAsync(jobLog);
        }

        private static async Task SendWelcomeMessageAsync(ITurnContext turnContext)
        {
            foreach (var member in turnContext.Activity.MembersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync($"Welcome to RemiBet {member.Name}.\r\n " +
                        $"{WelcomeText}");
                }
            }
        }

        private async Task OnSystemActivityAsync(ITurnContext turnContext)
        {
            // On a job completed event, mark the job as complete and notify the user.
            if (turnContext.Activity.Type is ActivityTypes.Event)
            {
            }
            else if (turnContext.Activity.Type is ActivityTypes.ConversationUpdate)
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
