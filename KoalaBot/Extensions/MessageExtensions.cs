using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Extensions
{
    public static class MessageExtensions
    {
        /// <summary>
        /// Gets the member that sent the message.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static DiscordMember GetMember(this DiscordMessage message) => message.Channel.Guild.GetMember(message.Author);

        /// <summary>
        /// Gets the member the sent the message.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Task<DiscordMember> GetMemberAsync(this DiscordMessage message) => message.Channel.Guild.GetMemberAsync(message.Author.Id);
    }
}
