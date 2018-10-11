using Microsoft.Bot.Builder;

namespace RemiBot
{
    public class JobState : BotState
    {
        private const string StorageKey = "RemiBot.JobState";

        public JobState(IStorage storage) 
            : base(storage, StorageKey)
        {
        }

        protected override string GetStorageKey(ITurnContext turnContext) => StorageKey;
    }
}
