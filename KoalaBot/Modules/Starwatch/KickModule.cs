using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using KoalaBot.Extensions;
using KoalaBot.Logging;
using KoalaBot.Redis;
using KoalaBot.Starwatch;
using KoalaBot.Starwatch.Entities;
using KoalaBot.Starwatch.Responses;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Modules.Starwatch
{
    public partial class StarwatchModule
    {
        [Group("kick")]
        public partial class KickModule : BaseCommandModule
        {
            public Koala Bot { get; }
            public IRedisClient Redis => Bot.Redis;
            public Logger Logger { get; }
            public StarwatchClient Starwatch => Bot.Starwatch;

            public KickModule(Koala bot)
            {
                this.Bot = bot;
                this.Logger = new Logger("CMD-SW-KICK", bot.Logger);
            }

            [Command("cid")]
            [Aliases("$")]
            public async Task KickCid(
                CommandContext ctx,
                [Description("CID to kick")] int cid,
                [Description("Reason")] string reason = null,
                [Description("Duration")] int? duration = null
            )
            {
                await ctx.ReplyWorkingAsync();

                Player p = new Player
                {
                    Connection = cid
                };

                var resp = await Starwatch.KickPlayerAsync(p, reason, duration);

                if (resp.Status != RestStatus.OK)
                {
                    await ctx.ReplyAsync($"{ctx.User.Mention}: Failed to kick CID '{cid}' - {resp.Message}");
                    return;
                }

                await ctx.ReplyAsync($"{ctx.User.Mention}: Kicked {resp.Payload.SuccessfulKicks} player(s).");
            }

            [Command("username"), Aliases("user", "name")]
            public async Task KickUsername(
                CommandContext ctx,
                [Description("Username to kick")] string username,
                [Description("Reason")] string reason = null,
                [Description("Duration")] int? duration = null
            )
            {
                await ctx.ReplyWorkingAsync();

                Player p = new Player
                {
                    Username = username
                };

                var resp = await Starwatch.KickPlayerAsync(p, reason, duration);

                if (resp.Status != RestStatus.OK)
                {
                    await ctx.ReplyAsync($"{ctx.User.Mention}: Failed to kick Username '{username}' - {resp.Message}");
                    return;
                }

                await ctx.ReplyAsync($"{ctx.User.Mention}: Kicked {resp.Payload.SuccessfulKicks} player(s).");
            }


            [Command("nickname"), Aliases("nick")]
            public async Task KickNickname(
                CommandContext ctx,
                [Description("Nickname to kick")] string nickname,
                [Description("Reason")] string reason = null,
                [Description("Duration")] int? duration = null
            )
            {
                await ctx.ReplyWorkingAsync();

                Player p = new Player
                {
                    Nickname = nickname
                };

                var resp = await Starwatch.KickPlayerAsync(p, reason, duration);

                if (resp.Status != RestStatus.OK)
                {
                    await ctx.ReplyAsync($"{ctx.User.Mention}: Failed to kick Nickname '{nickname}' - {resp.Message}");
                    return;
                }

                await ctx.ReplyAsync($"{ctx.User.Mention}: Kicked {resp.Payload.SuccessfulKicks} player(s).");
            }

            [Command("uuid")]
            public async Task KickUuid(
                CommandContext ctx,
                [Description("UUID to kick")] string uuid,
                [Description("Reason")] string reason = null,
                [Description("Duration")] int? duration = null
            )
            {
                await ctx.ReplyWorkingAsync();

                Player p = new Player
                {
                    UUID = uuid
                };

                var resp = await Starwatch.KickPlayerAsync(p, reason, duration);

                if (resp.Status != RestStatus.OK)
                {
                    await ctx.ReplyAsync($"{ctx.User.Mention}: Failed to kick UUID '{uuid}' - {resp.Message}");
                    return;
                }

                await ctx.ReplyAsync($"{ctx.User.Mention}: Kicked {resp.Payload.SuccessfulKicks} player(s).");
            }

            [Command("account"), Aliases("acc")]
            public async Task KickAccount(
                CommandContext ctx,
                [Description("Account to kick")] string account,
                [Description("Reason")] string reason = null,
                [Description("Duration")] int? duration = null
            )
            {
                await ctx.ReplyWorkingAsync();

                Player p = new Player
                {
                    AccountName = account
                };

                var resp = await Starwatch.KickPlayerAsync(p, reason, duration);

                if (resp.Status != RestStatus.OK)
                {
                    await ctx.ReplyAsync($"{ctx.User.Mention}: Failed to kick Account '{account}' - {resp.Message}");
                    return;
                }

                await ctx.ReplyAsync($"{ctx.User.Mention}: Kicked {resp.Payload.SuccessfulKicks} player(s).");
            }

            [Command("ip")]
            public async Task KickIp (
                CommandContext ctx, 
                [Description("IP to kick")] string ip,
                [Description("Reason")] string reason = null,
                [Description("Duration")] int? duration = null
            )
            {
                await ctx.ReplyWorkingAsync();

                Player p = new Player
                {
                    IP = ip
                };

                var resp = await Starwatch.KickPlayerAsync(p, reason, duration);

                if (resp.Status != RestStatus.OK)
                {
                    await ctx.ReplyAsync($"{ctx.User.Mention}: Failed to kick IP '{ip}' - {resp.Message}");
                    return;
                }

                await ctx.ReplyAsync($"{ctx.User.Mention}: Kicked {resp.Payload.SuccessfulKicks} player(s).");
            }
        }
    }
}
