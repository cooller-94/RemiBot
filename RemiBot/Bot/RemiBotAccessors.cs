using System;
using Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace Bot_Builder_Echo_Bot_V4
{
    public class RemiBotAccessors
    {
        public RemiBotAccessors(ConversationState conversationState, UserState userState)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            UserState = userState ?? throw new ArgumentNullException(nameof(userState));
        }

        public static string TopicStateName { get; } = $"{nameof(RemiBotAccessors)}.TopicState";

        public static string UserProfileName { get; } = $"{nameof(RemiBotAccessors)}.ProfileName";

        public static string CondversationDialogStateName { get; } = $"{nameof(RemiBotAccessors)}.DialogStateName";

        public IStatePropertyAccessor<TopicState> TopicStateAccessor { get; set; }

        public IStatePropertyAccessor<UserProfile> UserProfileAccessor { get; set; }

        public IStatePropertyAccessor<DialogState> ConversationDialogState { get; set; }

        public ConversationState ConversationState { get; }

        public UserState UserState { get; }
    }
}
