using System;
using Microsoft.Bot.Builder;

namespace Bot_Builder_Echo_Bot_V4
{
    public class RemiBotAccessors
    {
        public RemiBotAccessors(ConversationState conversationState)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
        }

        public static string CounterStateName { get; } = $"{nameof(RemiBotAccessors)}.CounterState";

        public IStatePropertyAccessor<CounterState> CounterState { get; set; }

        public ConversationState ConversationState { get; }
    }
}
