using DSharpPlus;
using KoalaBot.Database;
using KoalaBot.Logging;
using KoalaBot.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Managers
{
    public class Manager
    {
        public Logger Logger { get; }
        public Koala Bot { get; }

        public DiscordClient Discord => Bot.Discord;
        public IRedisClient Redis => Bot.Redis;
        public DbContext DbContext => Bot.DbContext;

        public Manager(Koala bot, Logger logger = null)
        {
            Bot = bot;
            Logger = logger ?? new Logger(GetType().Name);
        }
    }
}
