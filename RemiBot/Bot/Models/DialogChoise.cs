namespace Bot.Models
{
    public class DialogChoise
    {
        public DialogChoise()
        {
        }

        public DialogChoise(string description, string dialogName)
        {
            Description = description;
            DialogName = dialogName;
        }

        public string Description { get; set; }

        public string DialogName { get; set; }
    }
}
