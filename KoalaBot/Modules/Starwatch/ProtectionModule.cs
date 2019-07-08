using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using KoalaBot.Extensions;
using KoalaBot.Logging;
using KoalaBot.Permissions.CommandNext;
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
        [Group("protection"), Aliases("p")]
        [Description("Handles world protections, such as whitelist and blacklisting.")]
        public partial class ProtectionModule : BaseCommandModule
        {
            public Koala Bot { get; }
            public IRedisClient Redis => Bot.Redis;
            public Logger Logger { get; }
            public StarwatchClient Starwatch => Bot.Starwatch;

            public ProtectionModule(Koala bot)
            {
                this.Bot = bot;
                this.Logger = new Logger("CMD-SW-PROC", bot.Logger);
            }

            [GroupCommand]
            [Permission("sw.protection.list")]
            public async Task GetProtection(CommandContext ctx, World world)
            {
                if (world == null)
                    throw new ArgumentNullException("world");

                //Fetch the response
                await ctx.ReplyWorkingAsync();
                var response = await Starwatch.GetProtectionAsync(world);

                //404, the world doesn't exist
                if (response.Status == RestStatus.ResourceNotFound)
                {
                    await ctx.ReplyAsync(world + " does not have any protections.");
                    return;
                }

                //Something else, throw an exception
                if (response.Status != RestStatus.OK)
                    throw new RestResponseException(response);

                //Build the response
                DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
                builder.WithTitle($"Protection **{response.Payload.Name}**")
                    .AddField("Whereami", response.Payload.Whereami)
                    .AddField("Mode", response.Payload.Mode.ToString())
                    .AddField("Allow Anonymous", response.Payload.AllowAnonymous.ToString());

                await ctx.ReplyAsync(embed: builder.Build());
            }

            [Command("check"), Aliases("c", "accexist")]
            [Description("Checks the status of the account on the specified protection")]
            [Permission("sw.protection.check")]
            public async Task CheckAccount(CommandContext ctx, 
                [Description("The world to check")] World world,
                [Description("The account to check."), RemainingText] string account)
            {
                if (world == null)
                    throw new ArgumentNullException("world");

                if (string.IsNullOrWhiteSpace(account))
                    throw new ArgumentNullException("account");

                await ctx.ReplyWorkingAsync();
                var response = await Starwatch.GetProtectionAccountAsync(world, account);
                if (response.Status == RestStatus.ResourceNotFound || response.Payload == null)
                {
                    await ctx.ReplyReactionAsync(false);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(response.Payload.Reason))
                {
                    await ctx.ReplyAsync($"The account `{account}` was listed because: ```\n{response.Payload.Reason}\n```");
                }
                else
                {
                    await ctx.ReplyReactionAsync(true);
                }
            }

            [Command("add"), Aliases("addacc", "whitelist", "blacklist", "wl", "bl")]
            [Description("Adds an account to the specified world list.")]
            public async Task AddAccount(CommandContext ctx,
                [Description("The world to add the account too")] World world,
                [Description("The account to add."), RemainingText] string account)
            {

            }
        }
    }
}
