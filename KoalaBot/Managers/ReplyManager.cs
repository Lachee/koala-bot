using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using KoalaBot.Entities;
using KoalaBot.Extensions;
using KoalaBot.Logging;
using KoalaBot.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Managers
{
    public class ReplyManager : Manager
    {
        public TimeSpan ReplyTimeout { get; set; }

        //private Dictionary<ulong, Reply> _memcache;

        public ReplyManager(Koala bot, Logger logger = null) : base(bot, logger)
        {
            ReplyTimeout = TimeSpan.FromDays(1);

            //_memcache = new Dictionary<ulong, Reply>(16);

            Bot.Discord.MessageUpdated += HandleCommandsAsync;
            Bot.Discord.MessageDeleted += HandleMessageDelete;
        }

        private async Task HandleMessageDelete(DSharpPlus.EventArgs.MessageDeleteEventArgs e)
        {
            //Skip if its invalid
            if (e == null) return;
            if (e.Message == null) return;

            //Get the reply associated with the message
            var reply = await GetReplyFromEditAsync(e.Message);
            if (reply != null && reply.CommandMsg == e.Message.Id)
            {
                //We are just a message, so delete the message
                if (reply.ResponseType == Reply.SnowflakeType.Message)
                {
                    //Get the response message and load it into our memory cache
                    // then modify the contents of the message
                    var msg = await e.Channel.GetMessageAsync(reply.ResponseMsg);
                    if (msg != null) await msg.DeleteAsync("Deleted requesting method.");
                }
            }
        }

        private async Task HandleCommandsAsync(DSharpPlus.EventArgs.MessageUpdateEventArgs e)
        {
            //Skip if its invalid
            if (e == null) return;
            if (e.Author == null) return;
            if (e.Author.IsBot) return;

            //Resolve the prefix
            var mpos = e.Message.GetMentionPrefixLength(Bot.Discord.CurrentUser);
            if (mpos == -1) mpos = await Bot.ResolvePrefix(e.Message);
            if (mpos == -1) return;
            var pfx = e.Message.Content.Substring(0, mpos);

            //Prepare the command
            var command = e.Message.Content.Substring(mpos);
            var cmd = Bot.CommandsNext.FindCommand(command, out var args);
            if (cmd == null) return;

            //Make sure the user has permission to replay
            var member = e.Message.GetMember();
            if (member != null && await member.HasPermissionAsync("koala.reply." + e.Channel.Id))
            {
                //Create a context and execute the command
                var fctx = Bot.CommandsNext.CreateContext(e.Message, pfx, cmd, args);
                await Bot.CommandsNext.ExecuteCommandAsync(fctx).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Deletes all the responses in the specific channel
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public async Task DeleteResponsesAsync(DiscordChannel channel, int count = 10)
        {
            //if (!(Redis is StackExchangeClient))
            //    throw new Exception("The redis client must be a StackExchangeClient to utlise the SCAN.");
            //
            //var key = Namespace.Combine(channel.Guild, "replies", "*");
            //var redis = Redis as StackExchangeClient;
            //var server = redis.GetServersEnumable().First();

            //IEnumerable<DiscordMessage> deletes = server.Keys(pattern: key)
            //                                        .Select(k => redis.FetchStringAsync(key, "ResponseMsg", "0").Result)
            //                                        .Select(str => ulong.TryParse(str, out var id) ? id : 0)
            //                                        .Select(id => { try { return channel.GetMessageAsync(id).Result; } catch { return null; } })
            //                                        .Where(msg => msg != null);

            var messages = await channel.GetMessagesAsync();
            await channel.DeleteMessagesAsync(messages.Where(m => m.Author.Id == Bot.Discord.CurrentUser.Id).Take(count), "Cleanup");
        }

        /// <summary>
        /// Replies to a command context.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="content"></param>
        /// <param name="embed"></param>
        /// <returns></returns>
        public async Task<DiscordMessage> ReplyAsync(CommandContext ctx, string content = null, DiscordEmbed embed = null)
        {
            //Prepare the response
            DiscordMessage response = null;

            //This is a command, so lets make sure we have a response
            var reply = await GetReplyFromEditAsync(ctx.Message);
            if (reply != null && reply.CommandMsg == ctx.Message.Id)
            {
                //We are a reaction. We cannot create a new reaction, but we will remove our old one
                if (reply.ResponseType == Reply.SnowflakeType.Reaction)
                    await ctx.Message.DeleteOwnReactionAsync(DiscordEmoji.FromName(ctx.Client, reply.ResponseEmote));
                
                //We are just a message, so we will edit it
                if (reply.ResponseType == Reply.SnowflakeType.Message)
                {
                    //Get the response message and load it into our memory cache
                    // then modify the contents of the message
                    var msg = await ctx.Channel.GetMessageAsync(reply.ResponseMsg);
                    if (msg != null)
                        response = await msg.ModifyAsync(content, embed);
                }
               
                //Remove ourself from the cache, not longer required.
                //_memcache.Remove(ctx.Message.Id);
            }

            //We do not have a response, so just create a new one
            if (response == null)
                response = await ctx.RespondAsync(content: content, embed: embed);
            
            //Store the reply
            await StoreReplyAsync(ctx, response);
            return response;
        }

        /// <summary>
        /// Replies to a command context.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="content"></param>
        /// <param name="embed"></param>
        /// <returns></returns>
        public async Task ReactAsync(CommandContext ctx, DiscordEmoji reaction)
        {
            //If we have a response, check it
            var reply = await GetReplyFromEditAsync(ctx.Message);
            if (reply != null && reply.CommandMsg == ctx.Message.Id)
            {
                //We are a reaction. We cannot create a new reaction, but we will remove our old one
                if (reply.ResponseType == Reply.SnowflakeType.Reaction)
                {
                    await ctx.Message.DeleteOwnReactionAsync(DiscordEmoji.FromName(ctx.Client, reply.ResponseEmote));
                    await Task.Delay(250);
                }

                //We are just a message, so we will edit it
                if (reply.ResponseType == Reply.SnowflakeType.Message)
                {
                    //Get the response message and load it into our memory cache
                    // then modify the contents of the message
                    var msg = await ctx.Channel.GetMessageAsync(reply.ResponseMsg);
                    if (msg != null) await msg.DeleteAsync("Updated Response");
                }

                //Remove ourself from the cache, not longer required.
                //_memcache.Remove(ctx.Message.Id);
            }

            //We do not have a response, so just create a new one
            await ctx.Message.CreateReactionAsync(reaction);

            //Store the reply
            await StoreReactionAsync(ctx, reaction);
        }

        /// <summary>
        /// Gets the reply from the edited message
        /// </summary>
        /// <param name="editedMessage"></param>
        /// <returns></returns>
        public async Task<Reply> GetReplyFromEditAsync(DiscordMessage editedMessage)
        {
            return await GetReplyAsync(editedMessage.Channel.GuildId, editedMessage.Id);
        }

        /// <summary>
        /// Gets a reply given the guild and message id
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="messageId"></param>
        /// <returns></returns>
        public async Task<Reply> GetReplyAsync(ulong guildId, ulong messageId)
        {
            string key = GetReplyRedisNamespace(guildId, messageId);
            return await Redis.FetchObjectAsync<Reply>(key);
        }
        
        /// <summary>
        /// Gets the namespace that the reply would be stored under.
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="messageId"></param>
        /// <returns></returns>
        private string GetReplyRedisNamespace(ulong guildId, ulong messageId) => Namespace.Combine(guildId, "replies", messageId);

        /// <summary>
        /// Stores a reply reaction
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task StoreReplyAsync(CommandContext ctx, DiscordMessage message)
        {
            //Store the redis object
            string key = GetReplyRedisNamespace(ctx.Guild.Id, ctx.Message.Id);
            await Redis.StoreObjectAsync(key, new Reply()
            {
                CommandMsg = ctx.Message.Id,
                ResponseMsg = message.Id,
                ResponseType = Reply.SnowflakeType.Message
            });
            await Redis.SetExpiryAsync(key, ReplyTimeout);
        } 
        
        /// <summary>
        /// Stores a reply reaction
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task StoreReactionAsync(CommandContext ctx, DiscordEmoji reaction)
        {
            //Store the redis object
            string key = Namespace.Combine(ctx.Guild, "replies", ctx.Message);
            await Redis.StoreObjectAsync(key, new Reply()
            {
                CommandMsg = ctx.Message.Id,
                ResponseEmote = reaction.GetDiscordName(),
                ResponseType = Reply.SnowflakeType.Reaction
            });
            await Redis.SetExpiryAsync(key, ReplyTimeout);
        }
    }
}
