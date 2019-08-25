using KoalaBot.Database;
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

        public ConnectionSettings SQL { get; set; } = new ConnectionSettings()
        {
            Host = "192.168.1.20",
            Username = "root",
            Database = "koaladb",
            Password = "root",
            Prefix = "k_",
            DefaultImport = "koala.sql"
        };

        public StarwatchConfig Starwatch { get; set; } = new StarwatchConfig();
        public class StarwatchConfig
        {
            public string Host { get; set; } = "http://localhost:8000/";
            public string Username { get; set; } = "bot_example";
            public string Password { get; set; } = "someoriginalpassword";
        }

        public ulong ErrorWebhook { get; set; } = 545911071085428746L;
        public int MessageCounterSyncRate { get; set; } = 60;

      
    }

}
