using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace KoalaBot.Extensions
{
	public static class EmbedExtensions
	{
        public static string AvatarAPI { get; set; } = "https://d.lu.je/avatar/";

        public static bool IncludeBotAvatar { get; set; } = false;
		public static DiscordColor DefaultColour { get; set; } = new DiscordColor(12794879);
        public static DiscordColor ErrorColour { get; set; } = DiscordColor.DarkRed;
        public static DiscordColor WarningColour { get; set; } = DiscordColor.Orange;
        
        /// <summary>
        /// Turns the exception into a embeded format.
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="showStackTrace"></param>
        /// <returns></returns>
        public static DiscordEmbedBuilder ToEmbed(this Exception exception, bool showStackTrace = true)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.Color = ErrorColour;
            if (exception is AggregateException)
            {
                //Aggregate exceptions, we should log the first one only.
                AggregateException aggregate = exception as AggregateException;
                builder.Description = string.Format("An `AggregateException` has occured. Here is the first inner exception: ```{0}``` " + (showStackTrace ? "**Stacktrace** ```haskell\n{1}\n```" : ""),
                    aggregate.InnerException.Message, aggregate.InnerException.StackTrace);
            }
            else
            {
                builder.Description = string.Format("An exception has occured: ```{0}``` " + (showStackTrace ? "**Stacktrace** ```haskell\n{1}\n```" : ""), 
                    exception.Message, exception.StackTrace);
            }

            return builder;
		}
        
        /// <summary>
        /// Turns the command into a embeded response
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="title"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static DiscordEmbedBuilder ToEmbed(this CommandContext ctx, string title = null, DiscordColor? color = null)
        {
            if (title == null) title = ctx.Command.Name + " Response";
            if (title.Length > 1)
            {
                var c = title[0];
                title = c.ToString().ToUpperInvariant() + title.Substring(1);
            }

            return new DiscordEmbedBuilder()
                .WithFooter("Response to " + ctx.User.Username, AvatarAPI + ctx.User.Id)
                .WithAuthor(title, iconUrl: IncludeBotAvatar ? AvatarAPI + ctx.Client.CurrentUser.Id : null)
                .WithColor(color.GetValueOrDefault(DefaultColour))
                .WithTimestamp(DateTime.UtcNow);
        }

        public static DiscordEmbedBuilder WithDescription(this DiscordEmbedBuilder builder, string format, params object[] args) => builder.WithDescription(string.Format(format, args));
 
       

    }
}
