using DSharpPlus.Entities;
using KoalaBot.Extensions;
using KoalaBot.Logging;
using KoalaBot.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Managers
{
    public class ReactRoleManager : Manager
    {
        private const string REDIS_REACT_ROLE = "react-role";

        public ReactRoleManager(Koala bot, Logger logger = null) : base(bot, logger)
        {
            Discord.MessageDeleted += async (args) =>
            {
                //Remove the reactions that we stored.
                await RemoveAllReactionRoleAsync(args.Message);
            };

            Discord.MessagesBulkDeleted += async (args) =>
            {
                //Bulk deletes
                foreach (var m in args.Messages)
                    await RemoveAllReactionRoleAsync(m);
            };

            Discord.MessageReactionAdded += async (args) =>
            {
                //Award the reaction
                var r = await GetReactionRoleAsync(args.Message, args.Emoji);
                if (r != null)
                {
                    var gm = await args.Channel.Guild.GetMemberAsync(args.User);
                    await gm.GrantRoleAsync(r, "Reaction Role");
                }
            };

            Discord.MessageReactionRemoved += async (args) =>
            {
                //Remove the role
                var r = await GetReactionRoleAsync(args.Message, args.Emoji);
                var gm = await args.Channel.Guild.GetMemberAsync(args.User);
                await gm.RevokeRoleAsync(r, "Reaction Role");
            };
        }

        /// <summary>
        /// Gets a role that has been linked to a emoji
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="emoji"></param>
        /// <returns></returns>
        public async Task<DiscordRole> GetReactionRoleAsync(DiscordMessage message, DiscordEmoji emoji)
        {
            string key = Namespace.Combine(message.Channel.Guild, REDIS_REACT_ROLE, message);
            string val = await Redis.FetchStringAsync(key, emoji.Id.ToString(), null);
            if (string.IsNullOrEmpty(val)) return null;
            return message.Channel.Guild.GetRole(ulong.Parse(val));
        }

        /// <summary>
        /// Links a emoji to a role.
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="emoji"></param>
        /// <returns></returns>
        public async Task AddReactionRoleAsync(DiscordMessage message, DiscordEmoji emoji, DiscordRole role)
        {
            string key = Namespace.Combine(message.Channel.Guild, REDIS_REACT_ROLE, message);
            await Redis.StoreStringAsync(key, emoji.Id.ToString(), role.Id.ToString());
        }

        /// <summary>
        /// Unlinks a emoji from a role
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="emoji"></param>
        /// <returns></returns>
        public async Task<bool> RemoveReactionRoleAsync(DiscordMessage message, DiscordEmoji emoji)
        {
            string key = Namespace.Combine(message.Channel.Guild, REDIS_REACT_ROLE, message);
            return await Redis.RemoveHashSetAsync(key, emoji.Id.ToString());
        }

        /// <summary>
        /// Removes all the reaction roles. Doesn't remove any reactions already given.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> RemoveAllReactionRoleAsync(DiscordMessage message)
        {
            string key = Namespace.Combine(message.Channel.Guild, REDIS_REACT_ROLE, message);
            return await Redis.RemoveAsync(key);
        }
        
    }
}
