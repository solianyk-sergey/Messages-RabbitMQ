using System;

namespace Messages.Common.Messages
{
    [Serializable]
    public class SendSmsEvent
    {
        public int Id { get; set; }

        public string From { get; set; }
        public string To { get; set; }
        public string Body { get; set; }

        public string Version { get; set; }
    }
}
