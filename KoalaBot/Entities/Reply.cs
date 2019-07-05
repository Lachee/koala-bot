using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Entities
{
    public class Reply
    {
        public ulong CommandMsg { get; set; }
        public ulong ResponseMsg { get; set; }
        public string ResponseEmote { get; set; }
        public SnowflakeType ResponseType { get; set; }
        public enum SnowflakeType
        {
            Message,
            Reaction
        }
    }
}
