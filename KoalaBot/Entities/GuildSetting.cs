using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Extensions
{
    public class GuildSetting
    {
        public List<string> IgnoreChannels { get; set; }
        public string Prefix { get; set; }

        public GuildSetting()
        {
            IgnoreChannels = new List<string>();
            Prefix = "?";
        }
    }
}
