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
                var response = await Starwatch.UpdateAccountAsync(account, new Account() { IsActive = false});

                if (response.Status != RestStatus.OK)
                    throw new RestResponseException(response);

                //Build the response                
                await ctx.ReplyReactionAsync(!response.Payload.IsActive.GetValueOrDefault());
            }
            [Command("disable")]
            [Permission("sw.acc.disable")]
            [Description("Disables an account")]
            public async Task DisableAccount(CommandContext ctx, [Description("The name of the account to enable")] string account)
            {
                await ctx.ReplyWorkingAsync();
                var response = await Starwatch.UpdateAccountAsync(account, new Account() { IsActive = true });

                if (response.Status != RestStatus.OK)
                    throw new RestResponseException(response);

                //Build the response                
                await ctx.ReplyReactionAsync(response.Payload.IsActive.GetValueOrDefault());
            }

        }
    }
}
