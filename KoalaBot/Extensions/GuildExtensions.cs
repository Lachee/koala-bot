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

        /// <summary>
        /// Gets a member of this guild from a <see cref="DiscordUser"/> object.
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static Task<DiscordMember> GetMemberAsync(this DiscordGuild guild, DiscordUser user) => guild.GetMemberAsync(user.Id);

        /// <summary>
        /// Gets a member of this guild from a <see cref="DiscordUser"/> object.
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static DiscordMember GetMember(this DiscordGuild guild, DiscordUser user) => guild.Members[user.Id];
    }
}
