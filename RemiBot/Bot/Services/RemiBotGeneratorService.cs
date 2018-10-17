using System;
using System.Collections.Generic;
using System.Text;
using Bot.Models;

namespace Bot.Services
{
    public class RemiBotGeneratorService
    {
        public string GenerateNoteResponse(Note note, bool withTitle = true)
        {
            if (note == null)
            {
                throw new ArgumentNullException(nameof(note));
            }

            StringBuilder stringBuilder = new StringBuilder();

            if (withTitle)
            {
                stringBuilder.AppendLine($"**Title:** {note.Title}");
                stringBuilder.Append("\r\n");
            }

            stringBuilder.AppendLine($"**Note:** {note.Content}");

            return stringBuilder.ToString();
        }

        public string GenerateNoteListResponse(List<Note> notes, bool withTitle = true)
        {
            if (notes == null)
            {
                throw new ArgumentNullException(nameof(notes));
            }

            StringBuilder stringBuilder = new StringBuilder();

            foreach (Note item in notes)
            {
                string note = GenerateNoteResponse(item, withTitle);
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(note);
            }

            return stringBuilder.ToString();
        }

        public string GenerateHelpMessage()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("You can schedule a reminder or just leave a note. To interrupt any operation, press **cancel**:");

            return stringBuilder.ToString();
        }
    }
}
