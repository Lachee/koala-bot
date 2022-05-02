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
        [Group("announcement"), Aliases("an")]
        [Description("Announcements")]
        public partial class AnnouncementModule : BaseCommandModule
        {
            public Koala Bot { get; }
            public IRedisClient Redis => Bot.Redis;
            public Logger Logger { get; }
            public StarwatchClient Starwatch => Bot.Starwatch;

            public AnnouncementModule(Koala bot)
            {
                this.Bot = bot;
                this.Logger = new Logger("CMD-SW-ANNOUNCE", bot.Logger);
            }


            [Command("delete"), Aliases("-", "remove")]
            [Permission("sw.announce.delete")]
            [Description("Deletes an announcement")]
            public async Task DeleteAnnouncement(CommandContext ctx, [Description("The id of the announmcement")] long id)
            {

                await ctx.ReplyWorkingAsync();
                var response = await Starwatch.DeleteAnnouncementAsync(id);

                if (response.Status != RestStatus.OK)
                    throw new RestResponseException(response);

                //Build the response                
                await ctx.ReplyAsync("Deleted it.");
            }

            [Command("add"), Aliases("+")]
            [Permission("sw.announce.add")]
            [Description("Adds an announcement")]
            public async Task AddAnnouncement(CommandContext ctx,
                [Description("The message to RCON.")] string message,
                [Description("Whether to enable the announcement or not.")] bool enabled,
                [Description("Interval in seconds.")] double interval)
            {

                await ctx.ReplyWorkingAsync();
                var response = await Starwatch.PostAnnouncementAsync(message, enabled, interval);

                if (response.Status != RestStatus.OK)
                    throw new RestResponseException(response);

                //Build the response
                await ctx.ReplyAsync("Added it.");
            }

            [Command("enable")]
            [Permission("sw.announce.enable")]
            [Description("Enables an announcement.")]
            public async Task Task(CommandContext ctx,
                [Description("The ID of the announcement")] int id)
            {
                await ctx.ReplyWorkingAsync();

                AnnouncementPatch patch = new AnnouncementPatch
                {
                    Enabled = true
                };

                var response = await Starwatch.PutAnnouncementAsync(patch);

                if (response.Status != RestStatus.OK)
                    throw new RestResponseException(response);

                await ctx.ReplyAsync($"{ctx.User.Mention}: Enabled announcement #{id}");
            }

            [Command("disable")]
            [Permission("sw.announce.disable")]
            [Description("Disables an announcement.")]
            public async Task DisableAnnouncement(CommandContext ctx,
                [Description("The ID of the announcement")] int id)
            {
                await ctx.ReplyWorkingAsync();

                AnnouncementPatch patch = new AnnouncementPatch
                {
                    Enabled = false,
                    Id = id
                };

                var response = await Starwatch.PutAnnouncementAsync(patch);

                if (response.Status != RestStatus.OK)
                    throw new RestResponseException(response);

                await ctx.ReplyAsync($"{ctx.User.Mention}: Disabled announcement #{id}");
            }

            [Command("setmessage"), Aliases("setmsg")]
            [Permission("sw.announce.setmessage")]
            [Description("Sets an announcement's message")]
            public async Task SetAnnouncementMessage(CommandContext ctx,
                [Description("The ID of the announcement")] int id,
                [Description("The message of the announcement")] string message)
            {
                await ctx.ReplyWorkingAsync();

                AnnouncementPatch patch = new AnnouncementPatch
                {
                    Message = message,
                    Id = id
                };

                var response = await Starwatch.PutAnnouncementAsync(patch);

                if (response.Status != RestStatus.OK)
                    throw new RestResponseException(response);

                await ctx.ReplyAsync($"{ctx.User.Mention}: Set announcement message for announcement #{id}");
            }

            [Command("setinterval"), Aliases("setint", "timer", "settimer")]
            [Permission("sw.announce.setinterval")]
            [Description("Sets an announcement's timer interval")]
            public async Task SetAnnouncementMessage(CommandContext ctx,
                [Description("The ID of the announcement")] int id,
                [Description("The interval of the announcement in seconds")] int interval)
            {
                await ctx.ReplyWorkingAsync();

                AnnouncementPatch patch = new AnnouncementPatch
                {
                    Interval = interval,
                    Id = id
                };

                var response = await Starwatch.PutAnnouncementAsync(patch);

                if (response.Status != RestStatus.OK)
                    throw new RestResponseException(response);

                await ctx.ReplyAsync($"{ctx.User.Mention}: Set announcement interval for announcement #{id}");
            }

            [Command("get")]
            [Permission("sw.announcement.view")]
            [Description("Checks an announcement")]
            public async Task BanPlayer(CommandContext ctx,
                [Description("The id of the announcement")] long id)
            {
                //Fetch the response
                await ctx.ReplyWorkingAsync();

                //res
                var response = await Starwatch.GetAnnouncementAsync(id);

                //404, the world doesn't exist
                if (response.Status == RestStatus.ResourceNotFound)
                {
                    await ctx.ReplyAsync(id + " is not a valid announcement.");
                    return;
                }

                //Something else, throw an exception
                if (response.Status != RestStatus.OK)
                    throw new RestResponseException(response);

                //Build the response                
                await ctx.ReplyAsync(embed: BuildAnnouncementEmbed(response.Payload));
            }

            private DiscordEmbed BuildAnnouncementEmbed(Announcement announcement)
            {
                DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
                builder.WithTitle($"Announcement")
                    .AddField("Message", announcement.Message)
                    .AddField("Enabled", announcement.Enabled.ToString())
                    .AddField("Interval (seconds)", announcement.Interval.ToString());
                return builder.Build();
            }
        }
    }
}
