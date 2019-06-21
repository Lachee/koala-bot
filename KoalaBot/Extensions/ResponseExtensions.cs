using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Extensions
{
    public static class ResponseExtensions
    {
        /// <summary>
        /// Quickly responds to a command message with a reaction
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="emoji">The emoji to react with</param>
        /// <returns></returns>
        public static async Task<DiscordMessage> RespondReactionAsync(this CommandContext ctx, DiscordEmoji emoji)
        {
            await ctx.Message.CreateReactionAsync(emoji);
            return ctx.Message;
        }

        /// <summary>
        /// Quickly responds to a command message with a unicode reaction
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="reaction">The exact unicode character to react with</param>
        /// <returns></returns>
        public static async Task<DiscordMessage> RespondReactionAsync(this CommandContext ctx, string reaction) =>
            await RespondReactionAsync(ctx, DiscordEmoji.FromUnicode(reaction));

        /// <summary>
        /// Quickly responds to a command message with either a tick for success or a cross for failure
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="success">Was the command successful?</param>
        /// <returns></returns>
        public static async Task<DiscordMessage> RespondReactionAsync(this CommandContext ctx, bool success) =>
            await RespondReactionAsync(ctx, success ? "✅" : "❌");

        /// <summary>
        /// Quickly responds to the command with a embeded format
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static async Task<DiscordMessage> RespondAsEmbedAsync(this CommandContext ctx, string description) =>
            await ctx.RespondAsync(embed: ctx.ToEmbed().WithDescription(description));

        /// <summary>
        /// Quickly responds to the command with a embeded format
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static async Task<DiscordMessage> RespondAsEmbedAsync(this CommandContext ctx, string format, params object[] args) =>
            await ctx.RespondAsync(embed: ctx.ToEmbed().WithDescription(format, args));

        /// <summary>
        /// Quickly responds to the command with a exception dump
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="exception"></param>
        /// <param name="showStackTrace"></param>
        /// <returns></returns>
        public static async Task<DiscordMessage> RespondExceptionAsync(this CommandContext ctx, Exception exception, bool showStackTrace = true) =>
            await ctx.RespondAsync(embed: exception.ToEmbed(showStackTrace));

        /// <summary>
        /// Quickly responds to the command with a custom exception.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        public static async Task<DiscordMessage> RespondExceptionAsync(this CommandContext ctx, string exception) =>
            await ctx.RespondAsync(
                embed: ctx.ToEmbed()
                .WithDescription("An error has occured during the {0} command: ```\n{1}\n```", ctx.Command.Name, exception)
                .WithColor(EmbedExtensions.ErrorColour));        
    }
}
