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

                if (!await ctx.Member.HasPermissionAsync($"sw.protection.list.{world.Whereami}", false, allowUnset: true))
                    throw new PermissionException($"sw.protection.list.{world.Whereami}");

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
                builder.WithTitle($"Protection {response.Payload.Name}")
                    .AddField("Whereami", response.Payload.Whereami)
                    .AddField("Mode", response.Payload.Mode.ToString())
                    .AddField("Allow Anonymous", response.Payload.AllowAnonymous.ToString())
                    .WithColor(response.Payload.Mode == Protection.ProtectionMode.Blacklist ? DiscordColor.Black : DiscordColor.White);

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

                if (!await ctx.Member.HasPermissionAsync($"sw.protection.check.{world.Whereami}", false, allowUnset: true))
                    throw new PermissionException($"sw.protection.check.{world.Whereami}");

                //Get the response
                await ctx.ReplyWorkingAsync();
                var response = await Starwatch.GetProtectionAccountAsync(world, account);
                if (response.Status == RestStatus.ResourceNotFound || response.Payload == null)
                {
                    await ctx.ReplyReactionAsync(false);
                    return;
                }

                //Log with the reson, other just log they exist.
                if (!string.IsNullOrWhiteSpace(response.Payload.Reason))
                {
                    await ctx.ReplyAsync($"The account `{account}` was listed because: ```\n{response.Payload.Reason}\n```");
                }
                else
                {
                    await ctx.ReplyAsync($"The account `{account}` is listed.");
                }
            }

            [Command("add"), Aliases("addacc", "whitelist", "blacklist", "wl", "bl")]
            [Description("Adds an account to the specified world list.")]
            [Permission("sw.protection.add")]
            public async Task AddAccount(CommandContext ctx,
                [Description("The world to add the account too")] World world,
                [Description("The account to add."), RemainingText] string account)
            {

                if (world == null)
                    throw new ArgumentNullException("world");

                if (string.IsNullOrWhiteSpace(account))
                    throw new ArgumentNullException("account");

                if (!await ctx.Member.HasPermissionAsync($"sw.protection.add.{world.Whereami}", false, allowUnset: true))
                    throw new PermissionException($"sw.protection.add.{world.Whereami}");

                //Adds an account to the protection
                var response = await Starwatch.CreateProtectedAccountAsync(world, account, ctx.Member + " added user");
                await ctx.ReplyReactionAsync(response.Status != RestStatus.ResourceNotFound && response.Payload != null);
            }

            [Command("remove"), Aliases("remacc", "rem")]
            [Description("Removes an account to the specified world list.")]
            [Permission("sw.protection.remove")]
            public async Task RemoveAccount(CommandContext ctx,
                [Description("The world to add the account too")] World world,
                [Description("The account to add."), RemainingText] string account)
            {

                if (world == null)
                    throw new ArgumentNullException("world");

                if (string.IsNullOrWhiteSpace(account))
                    throw new ArgumentNullException("account");

               // if (!await ctx.Member.HasPermissionAsync($"sw.protection.remove.{world.Whereami}", false, allowUnset: true))
               //     throw new PermissionException($"sw.protection.remove.{world.Whereami}");

                //Removes an account from the list
                var response = await Starwatch.DeleteProtectedAccountAsync(world, account, ctx.Member + " removed user");
                await ctx.ReplyReactionAsync(response.Status == RestStatus.OK);
            }

            [Command("create"), Aliases("protect")]
            [Description("Creates or Edits a world protection")]
            [Permission("sw.protection.create")]
            public async Task CreateProtection(CommandContext ctx,
                [Description("The world to create the protection for")] World world,
                [Description("The mode of the world. Either BLACKLIST or WHITELIST")] string mode,
                [Description("Should anonymous connections be allowed to connect?")] bool allowAnonymous,
                [Description("A optional nickname for the protection")][RemainingText] string name = "")
            {

                if (world == null)
                    throw new ArgumentNullException("world");

                var response = await Starwatch.CreateProtectionAsync(new Protection()
                {
                    World = world,
                    Mode = mode.ToLowerInvariant() == "blacklist" ? Protection.ProtectionMode.Blacklist : Protection.ProtectionMode.Whitelist,
                    AllowAnonymous = allowAnonymous,
                    Name = name
                });

                await ctx.ReplyReactionAsync(response.Status != RestStatus.OK);
            }

            [Command("delete"), Aliases("unprotect")]
            [Description("Deletes a world protection")]
            [Permission("sw.protection.delete")]
            public async Task RemoveProtection(CommandContext ctx,
                [Description("The world to delete")] World world)
            {

                if (world == null)
                    throw new ArgumentNullException("world");

                var response = await Starwatch.DeleteProtectionAsync(world);
                await ctx.ReplyReactionAsync(response.Status != RestStatus.OK);
            }
        }
    }
}
