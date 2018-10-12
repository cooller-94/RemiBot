using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace Bot.Dialogs
{
    public class ReminderDialogs : DialogSet
    {
        public const string MainDialogId = "newNote";
        public const string TitlePromptId = "title";
        public const string ContentPormptId = "note";

        private IStatePropertyAccessor<UserProfile> _userProfileAccessor;

        public ReminderDialogs(IStatePropertyAccessor<DialogState> dialogStateAccessor, IStatePropertyAccessor<UserProfile> userProfileAccessor) 
            : base(dialogStateAccessor)
        {
            _userProfileAccessor = userProfileAccessor ?? throw new ArgumentNullException(nameof(userProfileAccessor));

            Add(new TextPrompt(TitlePromptId));
            Add(new TextPrompt(ContentPormptId));

            var waterfallSteps = new WaterfallStep[] { NoteTitleStepAsync, SaveTitleStepAsync, NoteContentStepAsync, SaveContentStepAsync };
            Add(new WaterfallDialog(MainDialogId, waterfallSteps));
        }

        private async Task<DialogTurnResult> NoteTitleStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync("title", new PromptOptions { Prompt = MessageFactory.Text("Please enter a title for a note.") }, cancellationToken);
        }

        private async Task<DialogTurnResult> SaveTitleStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string title = (string)stepContext.Result;

            var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

            Note note = new Note()
            {
                AddedDate = DateTime.Now,
                Title = title,
            };

            userProfile.Notes.Add(note);

            return await stepContext.ContinueDialogAsync(cancellationToken);
        }

        private async Task<DialogTurnResult> SaveContentStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string content = (string)stepContext.Result;
            var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

            Note note = userProfile.Notes.OrderByDescending(i => i.AddedDate).FirstOrDefault();

            if (note != null)
            {
                note.Content = content;

                await stepContext.Context.SendActivityAsync(MessageFactory.Text("The note has been successfully created."), cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken);
        }

        private async Task<DialogTurnResult> NoteContentStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync("note", new PromptOptions { Prompt = MessageFactory.Text("Please enter a note.") }, cancellationToken);
        }
    }
}
