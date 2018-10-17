using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bot.Dialogs;
using Bot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using RemiBot;

namespace Bot_Builder_Echo_Bot_V4
{
    public class RemiBot : IBot
    {
        private readonly RemiBotAccessors _accessors;
        private readonly ILogger _logger;

        private readonly EndpointService _endpointService;
        private readonly RemiBotGeneratorService _generatorService;
        private readonly RemiBotService _remiService;

        private ReminderDialogs _dialogs;

        public RemiBot(JobState jobState, EndpointService endpointService, RemiBotAccessors accessors, ILoggerFactory loggerFactory, RemiBotGeneratorService generatorService)
        {
            _accessors = accessors ?? throw new ArgumentNullException(nameof(accessors));
            _endpointService = endpointService;
            _generatorService = generatorService;
            _logger = loggerFactory.CreateLogger<RemiBot>();

            _dialogs = new ReminderDialogs(accessors.ConversationDialogState, accessors.UserProfileAccessor, generatorService, jobState, endpointService);
            _remiService = new RemiBotService(accessors, generatorService);

            _logger.LogTrace("RemiBot turn start");
        }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            DialogContext dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);

            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
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
                    await dialogContext.ContinueDialogAsync(cancellationToken);

                    if (!turnContext.Responded)
                    {
                        await dialogContext.BeginDialogAsync(ReminderDialogs.MainMenu, null, cancellationToken);
                    }
                }
            }
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                var activity = turnContext.Activity.AsConversationUpdateActivity();

                if (activity.MembersAdded.Any(member => member.Id != activity.Recipient.Id))
                {
                    await dialogContext.BeginDialogAsync(ReminderDialogs.MainMenu, null, cancellationToken);
                }
            }

            await _accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _accessors.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        private async Task OnCancelAsync(ITurnContext turnContext, DialogContext dialogContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync("Canceled.", cancellationToken: cancellationToken);
            await dialogContext.CancelAllDialogsAsync(cancellationToken);
            await dialogContext.BeginDialogAsync(ReminderDialogs.MainMenu, null, cancellationToken);
        }
    }
}
