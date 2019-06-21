using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KoalaBot
{
	public class BotConfig
	{
		public string Prefix { get; set; } = "\\";


        [JsonIgnore]
        public string Token { get { return File.ReadAllText(TokenFile); } }
        public string TokenFile { get; set; } = "discord.key";


        public string Resources { get; set; } = "Resources/";

        public RedisConfig Redis { get; set; } = new RedisConfig();
        public class RedisConfig
        {
            public string Address = "127.0.0.1";
            public int Database = 0;
            public string Prefix = "koala";
        }

        public SqlConfig SQL { get; set; } = new SqlConfig();
        public class SqlConfig
        {
            public string Address = "192.168.1.20";
            public string Username = "root";
            public string Password = "root";
            public string Database = "koaladb";
            public string Prefix = "k_";
        }

        public ulong ErrorWebhook { get; set; } = 545911071085428746L;
        public int MessageCounterSyncRate { get; set; } = 60;
        
	}

}
