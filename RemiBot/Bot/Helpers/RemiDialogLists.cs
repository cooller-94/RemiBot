using System.Collections.Generic;
using System.Linq;
using Bot.Constants;
using Bot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace Bot.Helpers
{
    public static class RemiDialogLists
    {
        public static List<DialogChoise> WelcomeOptions { get; } = new List<DialogChoise>
        {
            new DialogChoise { Description = "Note", DialogName = DialogConstants.NoteDialog },
            new DialogChoise { Description = "Reminder", DialogName = DialogConstants.ReminderDialog },
        };

        public static List<DialogChoise> NoteOptions { get; } = new List<DialogChoise>
        {
            new DialogChoise("New note", DialogConstants.CreateNoteDialog),
            new DialogChoise("Show lasts", DialogConstants.ShowLastNotesDialog),
            new DialogChoise("Delete note", DialogConstants.DeleteNoteDialog),
            new DialogChoise("Find note", DialogConstants.FindNote),
        };

        public static List<DialogChoise> ReminderOptions { get; } = new List<DialogChoise>
        {
            new DialogChoise("New reminder", DialogConstants.CreateReminderDialog),
        };

        private static readonly List<string> _welcomeList = WelcomeOptions.Select(x => x.Description).ToList();
        private static readonly List<string> _noteList = NoteOptions.Select(x => x.Description).ToList();
        private static readonly List<string> _reminderList = ReminderOptions.Select(x => x.Description).ToList();

        public static IList<Choice> WelcomeChoices { get; } = ChoiceFactory.ToChoices(_welcomeList);

        public static IList<Choice> NoteChoises { get; } = ChoiceFactory.ToChoices(_noteList);

        public static IList<Choice> ReminderChoises { get; } = ChoiceFactory.ToChoices(_reminderList);

        public static Activity WelcomeReprompt
        {
            get
            {
                var reprompt = MessageFactory.SuggestedActions(_welcomeList, "Please choose an option");
                reprompt.AttachmentLayout = AttachmentLayoutTypes.List;
                return reprompt as Activity;
            }
        }
    }
}
