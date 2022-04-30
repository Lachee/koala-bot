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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Modules.Starwatch
{
    public partial class StarwatchModule
    {
        [Group("account"), Aliases("acc")]
        [Description("Handles accounts.")]
        public partial class AccountModule : BaseCommandModule
        {
            public Koala Bot { get; }
            public IRedisClient Redis => Bot.Redis;
            public Logger Logger { get; }
            public StarwatchClient Starwatch => Bot.Starwatch;

            public AccountModule(Koala bot)
            {
                this.Bot = bot;
                this.Logger = new Logger("CMD-SW-ACC", bot.Logger);
            }

            [Command("enable")]
            [Permission("sw.acc.enable")]
            [Description("Enables an account")]
            public async Task EnableAccount(CommandContext ctx, [Description("The name of the account to enable")] string account)
            {
                await ctx.ReplyWorkingAsync();
                var response = await Starwatch.UpdateAccountAsync(account, new Account() { IsActive = true});

                if (response.Status != RestStatus.OK)
                    throw new RestResponseException(response);

                //Build the response                
                await ctx.ReplyReactionAsync(true);
            }

            [Command("disable")]
            [Permission("sw.acc.disable")]
            [Description("Disables an account")]
            public async Task DisableAccount(CommandContext ctx, [Description("The name of the account to enable")] string account)
            {
                await ctx.ReplyWorkingAsync();
                var response = await Starwatch.UpdateAccountAsync(account, new Account() { IsActive = false });

                if (response.Status != RestStatus.OK)
                    throw new RestResponseException(response);

                //Build the response                
                await ctx.ReplyReactionAsync(true);
            }

            [Command("create")]
            [Permission("sw.acc.create")]
            [Description("Creates a normal account")]
            public async Task CreateAccount(CommandContext ctx,
                [Description("The username")] string username,
                [Description("The password")] string password,
                [Description("Make admin?")] string makeAdmin = "no"
            )
            {
                await ctx.ReplyWorkingAsync();

                makeAdmin.ToCommandBool(out bool bMakeAdmin, false);
                if (bMakeAdmin)
                {
                    bool hasPerms = await ctx.Member.HasPermissionAsync("sw.acc.createadmin");
                    if (!hasPerms)
                    {
                        Logger.LogError($"{ctx.User.Username}#{ctx.User.Discriminator} tried to create an admin account with no permission.");
                        await ctx.ReplyDeleteAsync($"{ctx.User.Mention}: You're missing the ``sw.acc.createadmin`` permission.");
                        return;
                    }
                }

                Account account = new Account
                {
                    IsActive = true,
                    IsAdmin = bMakeAdmin,
                    Name = username,
                    Password = password
                };

                try
                {
                    var resp = await Starwatch.CreateAccountAsync(account);

                    // Success
                    if (resp.Status == RestStatus.OK)
                        await ctx.ReplyDeleteAsync($"{ctx.User.Mention} created an account '{username}'");

                    // User likely already exists.
                    else if (resp.Status == RestStatus.BadRequest && !(resp.Message is null))
                        await ctx.ReplyDeleteAsync($"{ctx.User.Mention}: {resp.Message}");

                    // Something wrong happened, maybe SSL?
                    else if (!(resp.Message is null))
                        await ctx.ReplyDeleteAsync($"{ctx.User.Mention}: Could not perform that action: {resp.Status.ToString()} - {resp.Message}");

                    // Something pretty wrong happened.
                    else
                        await ctx.ReplyDeleteAsync($"{ctx.User.Mention}: Could not perform that action: {resp.Status.ToString()}");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Caused by: {ctx.User.Mention} - Exception occurred with creating an account.");
                    throw ex;
                }
            }

            [Command("delete")]
            [Permission("sw.acc.delete")]
            [Description("Deletes an account")]
            public async Task DeleteAccount(CommandContext ctx,
                [Description("The username")] string username)
            {
                await ctx.ReplyWorkingAsync();

                try
                {
                    var resp = await Starwatch.DeleteAccountAsync(username);

                    if (resp.Status == RestStatus.OK)
                        await ctx.ReplyAsync($"{ctx.User.Mention} deleted account '{username}'");

                    else if (!(resp.Message is null))
                        await ctx.ReplyAsync($"{ctx.User.Mention}: Could not delete the user '{username}': {resp.Status.ToString()} - {resp.Message}");

                    else
                        await ctx.ReplyAsync($"{ctx.User.Mention}: Could not delete the user '{username}': {resp.Status.ToString()}");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Caused by: {ctx.User.Mention} - Exception occurred with deleting an account.");
                    throw ex;
                }
            }

            [Command("promote")]
            [Permission("sw.acc.promote")]
            [Description("Promotes a user to admin.")]
            public async Task PromoteAccount(CommandContext ctx,
                [Description("The username")] string username)
            {
                await ctx.ReplyWorkingAsync();

                Account account = new Account
                {
                    IsAdmin = true
                };

                try
                {
                    var resp = await Starwatch.UpdateAccountAsync(username, account);

                    if (resp.Status == RestStatus.OK)
                        await ctx.ReplyAsync($"{ctx.User.Mention} promoted account '{username}'");

                    else if (!(resp.Message is null))
                        await ctx.ReplyAsync($"{ctx.User.Mention}: Could not promote the user '{username}': {resp.Status.ToString()} - {resp.Message}");

                    else
                        await ctx.ReplyAsync($"{ctx.User.Mention}: Could not promote the user '{username}': {resp.Status.ToString()}");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Caused by: {ctx.User.Mention} - Exception occurred with promoting an account.");
                    throw ex;
                }
            }

            [Command("demote")]
            [Permission("sw.acc.demote")]
            [Description("Demotes a user to normal.")]
            public async Task DemoteAccount(CommandContext ctx,
                [Description("The username")] string username)
            {
                await ctx.ReplyWorkingAsync();

                Account account = new Account
                {
                    IsAdmin = false
                };

                try
                {
                    var resp = await Starwatch.UpdateAccountAsync(username, account);

                    if (resp.Status == RestStatus.OK)
                        await ctx.ReplyAsync($"{ctx.User.Mention} demoted account '{username}'");

                    else if (!(resp.Message is null))
                        await ctx.ReplyAsync($"{ctx.User.Mention}: Could not demote the user '{username}': {resp.Status.ToString()} - {resp.Message}");

                    else
                        await ctx.ReplyAsync($"{ctx.User.Mention}: Could not demote the user '{username}': {resp.Status.ToString()}");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Caused by: {ctx.User.Mention} - Exception occurred with demoting an account.");
                    throw ex;
                }
            }
        }
    }
}
