using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Extensions
{
    public static class ReplyExtensions
    {
        /// <summary>
        /// Responds to a message.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="content"></param>
        /// <param name="embed"></param>
        /// <returns></returns>
        public static async Task<DiscordMessage> ReplyAsync(this CommandContext ctx, string content = null, DiscordEmbed embed = null)
        {
            return await Koala.Bot.ReplyManager.ReplyAsync(ctx, content, embed);
        }

        /// <summary>
        /// Quickly responds to a command message with a reaction
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="emoji">The emoji to react with</param>
        /// <returns></returns>
        public static async Task<DiscordMessage> ReplyReactionAsync(this CommandContext ctx, DiscordEmoji emoji)
        {
            await Koala.Bot.ReplyManager.ReactAsync(ctx, emoji);
            return ctx.Message;
        }

        /// <summary>
        /// Quickly responds to a command message with a unicode reaction
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="reaction">The exact unicode character to react with</param>
        /// <returns></returns>
        public static async Task<DiscordMessage> ReplyReactionAsync(this CommandContext ctx, string reaction) =>
            await ReplyReactionAsync(ctx, DiscordEmoji.FromUnicode(reaction));

        /// <summary>
        /// Quickly responds to a command message with either a tick for success or a cross for failure
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="success">Was the command successful?</param>
        /// <returns></returns>
        public static async Task<DiscordMessage> ReplyReactionAsync(this CommandContext ctx, bool success) =>
            await ReplyReactionAsync(ctx, success ? "✅" : "❌");

        /// <summary>
        /// Quickly responds to a command message with a hour glass, to indicate its working.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public static async Task<DiscordMessage> ReplyWorkingAsync(this CommandContext ctx) =>
            await ReplyReactionAsync(ctx, "⌛");

        /// <summary>
        /// Quickly responds to the command with a embeded format
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static async Task<DiscordMessage> ReplyAsEmbedAsync(this CommandContext ctx, string description) =>
            await ctx.ReplyAsync(embed: ctx.ToEmbed().WithDescription(description));

        /// <summary>
        /// Quickly responds to the command with a embeded format
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static async Task<DiscordMessage> ReplyAsEmbedAsync(this CommandContext ctx, string format, params object[] args) =>
            await ctx.ReplyAsync(embed: ctx.ToEmbed().WithDescription(format, args));

        /// <summary>
        /// Quickly responds to the command with a exception dump
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="exception"></param>
        /// <param name="showStackTrace"></param>
        /// <returns></returns>
        public static async Task<DiscordMessage> ReplyExceptionAsync(this CommandContext ctx, Exception exception, bool showStackTrace = true) =>
            await ctx.ReplyAsync(embed: exception.ToEmbed(showStackTrace));

        /// <summary>
        /// Quickly responds to the command with a custom exception.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        public static async Task<DiscordMessage> ReplyExceptionAsync(this CommandContext ctx, string exception) =>
            await ctx.ReplyAsync(
                embed: ctx.ToEmbed()
                .WithDescription("An error has occured during the {0} command: ```\n{1}\n```", ctx.Command.Name, exception)
                .WithColor(EmbedExtensions.ErrorColour));

        /// <summary>
        /// Replys with a paginated message
        /// </summary>
        /// <param name="c"></param>
        /// <param name="u"></param>
        /// <param name="pages"></param>
        /// <param name="emojis"></param>
        /// <param name="behaviour"></param>
        /// <param name="deletion"></param>
        /// <param name="timeoutoverride"></param>
        /// <returns></returns>
        public static async Task ReplyPaginatedAsync(this CommandContext ctx,  IEnumerable<Page> pages, 
            PaginationEmojis emojis = null, 
            PaginationBehaviour behaviour = PaginationBehaviour.WrapAround, 
            PaginationDeletion deletion = PaginationDeletion.KeepEmojis, 
            TimeSpan? timeoutoverride = null)
        {
            var m = await ctx.ReplyAsync(pages.First().Content, pages.First().Embed);
            var timeout = timeoutoverride ?? TimeSpan.FromMinutes(1);

            var ems = emojis ?? new PaginationEmojis();
            var prequest = new KoalaBot.Interactivity.PaginationRequest(m, ctx.Member, behaviour, deletion, ems, timeout, pages.ToArray());
            await ctx.Client.GetInteractivity().WaitForCustomPaginationAsync(prequest);
        }
    }
}
