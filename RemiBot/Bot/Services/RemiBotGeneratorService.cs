using System;
using System.Text;
using Bot.Models;

namespace Bot.Services
{
    public class RemiBotGeneratorService
    {
        public string GenerateNoteResponse(Note note)
        {
            if (note == null)
            {
                throw new ArgumentNullException(nameof(note));
            }

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"**Title:** {note.Title}");
            stringBuilder.Append("\r\n");
            stringBuilder.AppendLine($"**Note:** {note.Content}");

            return stringBuilder.ToString();
        }
    }
}
