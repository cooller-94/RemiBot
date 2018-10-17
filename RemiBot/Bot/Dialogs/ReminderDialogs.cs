using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bot.Constants;
using Bot.Helpers;
using Bot.Models;
using Bot.Services;
using Hangfire;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using RemiBod;
using RemiBot;

namespace Bot.Dialogs
{
    public class ReminderDialogs : DialogSet
    {
        public const string MainMenu = "mainMenu";

        private static IStatePropertyAccessor<UserProfile> _userProfileAccessor;
        private static RemiBotGeneratorService _generatorService;
        private static EndpointService _endpointService;
        private static JobState _jobState;
        private static IStatePropertyAccessor<JobLog> _jobLogPropertyAccessor;

        public ReminderDialogs(IStatePropertyAccessor<DialogState> dialogStateAccessor, IStatePropertyAccessor<UserProfile> userProfileAccessor, RemiBotGeneratorService generatorService, JobState jobState, EndpointService endpointService)
            : base(dialogStateAccessor)
        {
            _userProfileAccessor = userProfileAccessor ?? throw new ArgumentNullException(nameof(userProfileAccessor));
            _generatorService = generatorService;
            _endpointService = endpointService;

            _jobState = jobState ?? throw new ArgumentNullException(nameof(jobState));
            _jobLogPropertyAccessor = _jobState.CreateProperty<JobLog>(nameof(JobLog));

            InitializationDialog();
        }

        private static class MainDialogSteps
        {
            public static async Task<DialogTurnResult> PresentMenuAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                PromptOptions options = new PromptOptions
                {
                    Prompt = MessageFactory.Text("How can I help you??"),
                    RetryPrompt = RemiDialogLists.WelcomeReprompt,
                    Choices = RemiDialogLists.WelcomeChoices,
                };

                return await stepContext.PromptAsync(DialogInputConstants.Choice, options, cancellationToken);
            }

            public static async Task<DialogTurnResult> ProcessInputAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                var choice = (FoundChoice)stepContext.Result;
                var dialogId = RemiDialogLists.WelcomeOptions[choice.Index].DialogName;

                return await stepContext.BeginDialogAsync(dialogId, null, cancellationToken);
            }

            public static async Task<DialogTurnResult> RepeatMenuAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                return await stepContext.ReplaceDialogAsync(MainMenu, null, cancellationToken);
            }
        }

        private static class NoteDialogMenuSteps
        {
            public static async Task<DialogTurnResult> PresentMenuAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                PromptOptions options = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please select an option"),
                    RetryPrompt = MessageFactory.Text("Please select an option"),
                    Choices = RemiDialogLists.NoteChoises,
                };

                return await stepContext.PromptAsync(DialogInputConstants.Choice, options, cancellationToken);
            }

            public static async Task<DialogTurnResult> ProcessMenuAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                var choice = (FoundChoice)stepContext.Result;
                var dialogId = RemiDialogLists.NoteOptions[choice.Index].DialogName;

                return await stepContext.BeginDialogAsync(dialogId, null, cancellationToken);
            }

            public static async Task<DialogTurnResult> RepeatMenuAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                return await stepContext.ReplaceDialogAsync(MainMenu, null, cancellationToken);
            }
        }

        private static class ReminderDialogMenuSteps
        {
            public static async Task<DialogTurnResult> PresentMenuAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                PromptOptions options = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please select an option"),
                    RetryPrompt = MessageFactory.Text("Please select an option"),
                    Choices = RemiDialogLists.ReminderChoises,
                };

                return await stepContext.PromptAsync(DialogInputConstants.Choice, options, cancellationToken);
            }

            public static async Task<DialogTurnResult> ProcessMenuAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                var choice = (FoundChoice)stepContext.Result;
                var dialogId = RemiDialogLists.ReminderOptions[choice.Index].DialogName;

                return await stepContext.BeginDialogAsync(dialogId, null, cancellationToken);
            }

            public static async Task<DialogTurnResult> RepeatMenuAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                return await stepContext.ReplaceDialogAsync(MainMenu, null, cancellationToken);
            }
        }

        private static class CreateNoteDialogMenuSteps
        {
            public static async Task<DialogTurnResult> GetTitleStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                return await stepContext.PromptAsync(DialogInputConstants.Text, new PromptOptions { Prompt = MessageFactory.Text("Please enter a title for a note.") }, cancellationToken);
            }

            public static async Task<DialogTurnResult> GetNoteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                string title = (string)stepContext.Result;
                stepContext.Values[DialogOutputConstants.NoteTitle] = title;

                return await stepContext.PromptAsync(DialogInputConstants.Text, new PromptOptions { Prompt = MessageFactory.Text("Please enter a note.") }, cancellationToken);
            }

            public static async Task<DialogTurnResult> ProcessNoteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                string content = (string)stepContext.Result;
                string title = (string)stepContext.Values[DialogOutputConstants.NoteTitle];

                var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

                Note note = new Note()
                {
                    AddedDate = DateTime.Now,
                    Title = title,
                    Content = content,
                };

                userProfile.Notes.Add(note);

                await stepContext.Context.SendActivityAsync(MessageFactory.Text("The note has been successfully created."), cancellationToken);

                return await stepContext.EndDialogAsync(cancellationToken);
            }
        }

        private static class ShowLastNotesSteps
        {
            public static async Task<DialogTurnResult> GetCountDisplayedNotesAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                PromptOptions options = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please enter count notes to display"),
                    RetryPrompt = MessageFactory.Text("Please enter a valid count"),
                };

                return await stepContext.PromptAsync(DialogInputConstants.Number, options, cancellationToken);
            }

            public static async Task<DialogTurnResult> ProcessDisplayingLastNotesAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                int count = (int)stepContext.Result;
                var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

                List<Note> notes = userProfile.Notes;

                if (notes.Count == 0)
                {
                    await stepContext.Context.SendActivityAsync("You do not have any note");
                    return await stepContext.EndDialogAsync(null, cancellationToken);
                }

                if (count > notes.Count)
                {
                    await stepContext.Context.SendActivityAsync($"There are only {notes.Count} notes:");
                }

                await stepContext.Context.SendActivityAsync(_generatorService.GenerateNoteListResponse(notes.Take(count).ToList()));
                return await stepContext.EndDialogAsync(cancellationToken);
            }
        }

        private static class DeleteNoteSteps
        {
            public static async Task<DialogTurnResult> GetTitleToDeleteAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                PromptOptions options = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please enter a title to delete a note"),
                    RetryPrompt = MessageFactory.Text("Please enter a title to delete a note"),
                };

                return await stepContext.PromptAsync(DialogInputConstants.Text, options, cancellationToken);
            }

            public static async Task<DialogTurnResult> ConfirmDeleteNoteAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                string title = (string)stepContext.Result;

                var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

                List<Note> notes = userProfile.Notes.Where(i => i.Title.Equals(title)).ToList();

                if (notes.Count == 0)
                {
                    await stepContext.Context.SendActivityAsync($"There are no notes with **{title}** title");
                    return await stepContext.ReplaceDialogAsync(DialogConstants.DeleteNoteDialog, null, cancellationToken);
                }

                stepContext.Values[DialogOutputConstants.NoteTitle] = title;

                string confirmMessage = $"Would you like to delete the following {(notes.Count > 1 ? "notes" : "note")}: \r\n {_generatorService.GenerateNoteListResponse(notes, false)}";

                return await stepContext.PromptAsync(DialogInputConstants.Confirm, new PromptOptions() { Prompt = MessageFactory.Text(confirmMessage), RetryPrompt = MessageFactory.Text(confirmMessage), });
            }

            public static async Task<DialogTurnResult> ProcessDeleteNoteAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                if ((bool)stepContext.Result)
                {
                    var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
                    string title = (string)stepContext.Values[DialogOutputConstants.NoteTitle];

                    userProfile.Notes.RemoveAll(i => i.Title == title);
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Successfully deleted!"), cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Thanks. Your notes cannot be deleted!"), cancellationToken);
                }

                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }

        private static class FindDialogSteps
        {
            public static async Task<DialogTurnResult> GetTermToSearchAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                PromptOptions options = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please enter a term to search."),
                    RetryPrompt = MessageFactory.Text("Please enter a term to search. It's can be a full title or part of title/note"),
                };

                return await stepContext.PromptAsync(DialogInputConstants.Text, options, cancellationToken);
            }

            public static async Task<DialogTurnResult> ProcessSearchNoteAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                string term = (string)stepContext.Result;

                var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

                List<Note> notes = userProfile.Notes.Where(i => (i.Title != null && i.Title.Contains(term)) || (i.Content != null && i.Content.Contains(term))).Select(i => (Note)i.Clone()).ToList();

                if (notes.Count == 0)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("There are no notes for this term."), cancellationToken);
                    return await stepContext.ReplaceDialogAsync(DialogConstants.FindNote, null, cancellationToken);
                }

                foreach (Note note in notes)
                {
                    note.Title = note.Title.Replace(term, $"*{term}*");
                    note.Content = note.Content.Replace(term, $"*{term}*");
                }

                await stepContext.Context.SendActivityAsync(MessageFactory.Text(_generatorService.GenerateNoteListResponse(notes)), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }

        private static class CreateReminderSteps
        {
            public static async Task<DialogTurnResult> GetReminderDescriptionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                PromptOptions options = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please enter a reminder."),
                    RetryPrompt = MessageFactory.Text("Please enter a reminder."),
                };

                return await stepContext.PromptAsync(DialogInputConstants.Text, options, cancellationToken);
            }

            public static async Task<DialogTurnResult> GetDateAndTimeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                string reminderName = (string)stepContext.Result;
                stepContext.Values[DialogOutputConstants.ReminderName] = reminderName;

                PromptOptions options = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please enter a date."),
                    RetryPrompt = MessageFactory.Text("Please enter a valid date and time."),
                };

                return await stepContext.PromptAsync(DialogInputConstants.DateTime, options, cancellationToken);
            }

            public static async Task<DialogTurnResult> ProcessReminderAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                string dateTimeString = ((IList<DateTimeResolution>)stepContext.Result).FirstOrDefault().Value;

                if (DateTime.TryParse(dateTimeString, out DateTime dateTime))
                {
                    JobLog jobLog = await _jobLogPropertyAccessor.GetAsync(stepContext.Context, () => new JobLog());
                    await OnNewReminderAsync(stepContext.Context, jobLog, dateTime, (string)stepContext.Values[DialogOutputConstants.ReminderName]);
                }

                return await stepContext.ReplaceDialogAsync(MainMenu, cancellationToken: cancellationToken);
            }
        }

        public static async Task RunJobAsync(JobLog.JobData jobLog)
        {
            await CompleteJobAsync(jobLog);
        }

        private static async Task OnNewReminderAsync(ITurnContext turnContext, JobLog jobLog, DateTime dateTime, string reminder)
        {
            JobLog.JobData job = SetReminder(turnContext, jobLog, dateTime, reminder);

            await _jobLogPropertyAccessor.SetAsync(turnContext, jobLog);
            await _jobState.SaveChangesAsync(turnContext);

            await turnContext.SendActivityAsync($"Thanks! The reminder was scheduled on {dateTime.ToString()}");
        }

        private static JobLog.JobData SetReminder(ITurnContext turnContext, JobLog jobLog, DateTime dateTime, string reminder)
        {
            JobLog.JobData jobInfo = new JobLog.JobData
            {
                TimeStamp = DateTime.Now.ToBinary(),
                Conversation = turnContext.Activity.GetConversationReference(),
                Text = reminder,
            };

            jobLog[jobInfo.TimeStamp] = jobInfo;

            BackgroundJob.Schedule(() => RunJobAsync(jobInfo), dateTime);

            return jobInfo;
        }

        // Sends a proactive message to the user.
        private static async Task CompleteJobAsync(JobLog.JobData jobInfo, CancellationToken cancellationToken = default(CancellationToken))
        {
            IMessageActivity message = Activity.CreateMessageActivity();

            message.ChannelId = jobInfo.Conversation.ChannelId;
            message.From = jobInfo.Conversation.Bot;
            message.Recipient = jobInfo.Conversation.User;
            message.Conversation = jobInfo.Conversation.Conversation;
            message.Text = jobInfo.Text;
            message.Locale = "en-us";

            var connector = new ConnectorClient(new Uri(jobInfo.Conversation.ServiceUrl), _endpointService.AppId, _endpointService.AppPassword);
            await connector.Conversations.SendToConversationAsync((Activity)message);
        }

        private void InitializationDialog()
        {
            Add(new ChoicePrompt(DialogInputConstants.Choice));
            Add(new NumberPrompt<int>(DialogInputConstants.Number));
            Add(new TextPrompt(DialogInputConstants.Text));
            Add(new ConfirmPrompt(DialogInputConstants.Confirm));
            Add(new DateTimePrompt(DialogInputConstants.DateTime, DateTimeValidatorAsync));

            WaterfallStep[] welcomeDialogSteps = new WaterfallStep[]
            {
                MainDialogSteps.PresentMenuAsync, MainDialogSteps.ProcessInputAsync, MainDialogSteps.RepeatMenuAsync,
            };

            Add(new WaterfallDialog(MainMenu, welcomeDialogSteps));

            WaterfallStep[] menuNoteSteps = new WaterfallStep[]
            {
                NoteDialogMenuSteps.PresentMenuAsync, NoteDialogMenuSteps.ProcessMenuAsync, NoteDialogMenuSteps.RepeatMenuAsync,
            };

            Add(new WaterfallDialog(DialogConstants.NoteDialog, menuNoteSteps));

            WaterfallStep[] createNoteSteps = new WaterfallStep[]
            {
                CreateNoteDialogMenuSteps.GetTitleStepAsync, CreateNoteDialogMenuSteps.GetNoteStepAsync, CreateNoteDialogMenuSteps.ProcessNoteStepAsync,
            };

            Add(new WaterfallDialog(DialogConstants.CreateNoteDialog, createNoteSteps));

            WaterfallStep[] showLastDialogSteps = new WaterfallStep[]
            {
               ShowLastNotesSteps.GetCountDisplayedNotesAsync, ShowLastNotesSteps.ProcessDisplayingLastNotesAsync,
            };

            Add(new WaterfallDialog(DialogConstants.ShowLastNotesDialog, showLastDialogSteps));

            WaterfallStep[] deleteNoteSteps = new WaterfallStep[]
            {
                DeleteNoteSteps.GetTitleToDeleteAsync, DeleteNoteSteps.ConfirmDeleteNoteAsync, DeleteNoteSteps.ProcessDeleteNoteAsync,
            };

            Add(new WaterfallDialog(DialogConstants.DeleteNoteDialog, deleteNoteSteps));

            WaterfallStep[] findNoteSteps = new WaterfallStep[]
            {
                FindDialogSteps.GetTermToSearchAsync, FindDialogSteps.ProcessSearchNoteAsync,
            };

            Add(new WaterfallDialog(DialogConstants.FindNote, findNoteSteps));

            WaterfallStep[] menuReminderSteps = new WaterfallStep[]
            {
              ReminderDialogMenuSteps.PresentMenuAsync, ReminderDialogMenuSteps.ProcessMenuAsync, ReminderDialogMenuSteps.RepeatMenuAsync,
            };

            Add(new WaterfallDialog(DialogConstants.ReminderDialog, menuReminderSteps));

            WaterfallStep[] createReminderSteps = new WaterfallStep[]
            {
               CreateReminderSteps.GetReminderDescriptionAsync, CreateReminderSteps.GetDateAndTimeAsync, CreateReminderSteps.ProcessReminderAsync,
            };

            Add(new WaterfallDialog(DialogConstants.CreateReminderDialog, createReminderSteps));
        }

        private Task<bool> DateTimeValidatorAsync(PromptValidatorContext<IList<DateTimeResolution>> prompt, CancellationToken cancellationToken)
        {
            if (prompt.Recognized.Succeeded)
            {
                var resolution = prompt.Recognized.Value.First();

                var now = DateTime.Now;
                DateTime.TryParse(resolution.Value, out var time);

                if (time < now)
                {
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}
