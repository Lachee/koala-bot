using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using KoalaBot.CommandNext;
using KoalaBot.Exceptions;
using KoalaBot.Extensions;
using KoalaBot.Logging;
using KoalaBot.Redis;
using KoalaBot.Starwatch;
using KoalaBot.Starwatch.Entities;
using KoalaBot.Starwatch.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Modules.Starwatch
{
    public partial class StarwatchModule
    {
        [Group("ban"), Aliases("b")]
        [Description("Handles world backups.")]
        public partial class BanModule : BaseCommandModule
        {
            public Koala Bot { get; }
            public IRedisClient Redis => Bot.Redis;
            public Logger Logger { get; }
            public StarwatchClient Starwatch => Bot.Starwatch;

            public BanModule(Koala bot)
            {
                this.Bot = bot;
                this.Logger = new Logger("CMD-SW-BAN", bot.Logger);
            }

            [Command("create"), Aliases("add", "+")]
            [Permission("sw.ban.ip")]
            [Description("Creates a new ban")]
            public async Task BanPlayer(CommandContext ctx, string ip, string reason)
            {
                if (string.IsNullOrWhiteSpace(ip))
                    throw new ArgumentNullException("ip");

                if (string.IsNullOrWhiteSpace(reason))
                    throw new ArgumentNullException("reason");

                //Fetch the response
                await ctx.ReplyWorkingAsync();
                var ban = new Ban()
                {
                    IP = ip,
                    Reason = reason,
                    Moderator = "Discord: " + ctx.Member.Username + "(" + ctx.Member.Id + ")",
                };

                //res
                var response = await Starwatch.BanAsync(ban);

                //Something else, throw an exception
                if (response.Status != RestStatus.OK)
                    throw new RestResponseException(response);

                //Build the response                
                await ctx.ReplyAsync(embed: BuildBanEmbed(response.Payload));
            }

            [Command("get"), Aliases("check", "isbanned")]
            [Permission("sw.ban.view")]
            [Description("Checks a ban")]
            public async Task BanPlayer(CommandContext ctx, long ticket)
            {
                //Fetch the response
                await ctx.ReplyWorkingAsync();

                //res
                var response = await Starwatch.GetBanAsync(ticket);

                //404, the world doesn't exist
                if (response.Status == RestStatus.ResourceNotFound)
                {
                    await ctx.ReplyAsync(ticket + " is not a valid ban.");
                    return;
                }

                //Something else, throw an exception
                if (response.Status != RestStatus.OK)
                    throw new RestResponseException(response);

                //Build the response                
                await ctx.ReplyAsync(embed: BuildBanEmbed(response.Payload));
            }

            private DiscordEmbed BuildBanEmbed(Ban ban)
            {
                DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
                builder.WithTitle($"Ban {ban.Ticket}")
                    .AddField("IP", ban.IP)
                    .AddField("UUID", ban.UUID)
                    .AddField("Reason", ban.Reason)
                    .AddField("Moderator", ban.Moderator)
                    .AddField("Ban Date", ban.CreatedDate.ToString());
                return builder.Build();
            }

        }
    }
}
