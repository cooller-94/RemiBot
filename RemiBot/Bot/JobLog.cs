using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace RemiBod
{
    public class JobLog : Dictionary<long, JobLog.JobData>
    {
        public class JobData
        {
            public long TimeStamp { get; set; } = 0;

            public bool Completed { get; set; } = false;

            public ConversationReference Conversation { get; set; }
            public string Text { get; set;  }
            public DateTime AlarmTime { get; set; }
        }
    }
}
