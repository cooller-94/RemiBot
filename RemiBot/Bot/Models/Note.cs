
using System;

namespace Bot.Models
{
    public class Note : ICloneable
    {
        public string Title { get; set; }

        public string Content { get; set; }
        public DateTime AddedDate { get; set; }

        public object Clone()
        {
            return new Note
            {
                Title = Title,
                Content = Content,
                AddedDate = AddedDate,
            };
        }
    }
}
