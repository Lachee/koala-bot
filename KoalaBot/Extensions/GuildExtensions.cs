using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Extensions
{
    public static class GuildExtensions
    {
        /// <summary>
        /// Gets the settings of the guild. It is not cached.
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public static Task<GuildSettings> GetSettingsAsync(this DiscordGuild guild) => GuildSettings.GetGuildSettingsAsync(guild);
    }
}
