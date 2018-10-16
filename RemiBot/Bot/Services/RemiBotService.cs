using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bot.Models;
using Bot_Builder_Echo_Bot_V4;
using Microsoft.Bot.Builder;

namespace Bot.Services
{
    public class RemiBotService
    {
        private readonly RemiBotAccessors _accessors;
        private readonly RemiBotGeneratorService _generatorService;

        public RemiBotService(RemiBotAccessors accessors, RemiBotGeneratorService generatorService)
        {
            _accessors = accessors;
            _generatorService = generatorService;
        }

        public async Task<string> GetShowLastNotesMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken, string utterance)
        {
            string numberString = Regex.Split(utterance, @"\D+").Where(i => !string.IsNullOrEmpty(i)).FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(numberString))
            {
                if (int.TryParse(numberString, out int showLastCount))
                {
                    var userProfile = await _accessors.UserProfileAccessor.GetAsync(turnContext, () => new UserProfile(), cancellationToken);
                    List<Note> notes = userProfile?.Notes?.OrderByDescending(i => i.AddedDate).Take(showLastCount).ToList();
                    return notes.Count == 0 ? "You do not have any note" : _generatorService.GenerateNoteListResponse(notes);
                }
            }

            return "Could not recognize  the command. Please use the **help** command to see all available command list.";
        }
    }
}
