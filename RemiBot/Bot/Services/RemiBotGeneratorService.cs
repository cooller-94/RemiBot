﻿using System;
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

        public string GenerateHelpMessage()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("You can use one of the following commands:");
            stringBuilder.AppendLine("\r\n");
            stringBuilder.AppendLine("\r\n\t");
            stringBuilder.AppendLine("**new note** - to create a new note");
            stringBuilder.AppendLine("\r\n\t");
            stringBuilder.AppendLine("**show last** - to display last added note");
            stringBuilder.AppendLine("\r\n\t");
            stringBuilder.AppendLine("**cancel** - to cancel current operation");
            stringBuilder.AppendLine("\r\n");
            stringBuilder.AppendLine("**help** - to display the list of commands");

            return stringBuilder.ToString();
        }
    }
}
