using System.Collections.Generic;
using Bot.Models;

namespace Bot
{
    public class UserProfile
    {
        public string UserName { get; set; }

        public List<Note> Notes { get; set; }

        public UserProfile()
        {
            Notes = new List<Note>();
        }
    }
}
