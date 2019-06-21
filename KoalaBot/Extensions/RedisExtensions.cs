using DSharpPlus.Entities;
using KoalaBot.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Extensions
{
    public static class RedisExtensions
    {
        public static Namespace ToNamespace(this DiscordGuild guild) => new Namespace(guild.Id);
    }
}
