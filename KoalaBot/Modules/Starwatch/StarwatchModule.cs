using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using KoalaBot.Redis;
using KoalaBot.Extensions;
using KoalaBot.Logging;
using System.IO;
using DSharpPlus.Entities;
using KoalaBot.Starwatch;
using KoalaBot.CommandNext;
using KoalaBot.Starwatch.Entities;
using DSharpPlus.Interactivity;
using KoalaBot.Util;

namespace KoalaBot.Modules.Starwatch
{
    [Group("starwatch"), Aliases("sw", "s")]
    [Permission("sw")]
    public partial class StarwatchModule : BaseCommandModule
    {
        public Koala Bot { get; }
        public IRedisClient Redis => Bot.Redis;
        public Logger Logger { get; }
        public StarwatchClient Starwatch => Bot.Starwatch;

        public StarwatchModule(Koala bot)
        {
            this.Bot = bot;
            this.Logger = new Logger("CMD-SW", bot.Logger);
        }

        [Command("reload")]
        [Description("Reloads the server")]
        [Permission("sw.reload")]
        public async Task ReloadServer(CommandContext ctx)
        {
            await ctx.ReplyReactionAsync("⌛");
            await Starwatch.ReloadAsync(false);
            await ctx.ReplyReactionAsync(true);
        }

        [Command("restart")]
        [Description("Restarts the server with a given reason.")]
        [Permission("sw.restart")]
        public async Task RestartServer(CommandContext ctx, [Description("Optional. Reason for the restart.")] [RemainingText] string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                reason = "Restarted by @" + ctx.Member.Nickname;

            await ctx.ReplyWorkingAsync();
            await Starwatch.RestartAsync(reason, false);
            await ctx.ReplyReactionAsync(true);
        }

        [Command("statistics"), Aliases("stat", "stats", "s")]
        [Description("Gets the statistics of the server")]
        [Permission("sw.statistics")]
        public async Task GetStatistics(CommandContext ctx)
        {
            await ctx.ReplyWorkingAsync();
            var response = await Starwatch.GetStatisticsAsync();
            var statistics = response.Payload;

            var embed = new DiscordEmbedBuilder();
            embed.WithTitle("Server Statistics");

            if (statistics.IsRunning)
                embed.WithColor(DiscordColor.Green);

            //Add the uptime
            string uptime = "";
            if (statistics.FormatUptime(out uptime)) embed.AddField("🕒 Uptime", uptime);
            else embed.AddField("☠ Downtime", uptime);

            //Add the current connections
            embed.AddField("Connections", statistics.Connections.ToString(), true);
            embed.AddField("Connections This Session", statistics.LastConnectionID.GetValueOrDefault(0).ToString(), true);

            //Add the memory usage
            embed.AddField("Cur Memory", string.Format("{0:n0}", statistics.Memory.WorkingSet / 1024 / 1024) + " MB", true);
            embed.AddField("Max Memory", string.Format("{0:n0}", statistics.Memory.PeakWorkingSet / 1024 / 1024) + " MB", true);

            await ctx.ReplyAsync(embed: embed.Build());
        }

        [Command("aliases")]
        [Description("Gets the aliases of a user")]
        [Permission("sw.alias")]
        public async Task SearchAliasSessions(CommandContext ctx, string name)
        {
            //TODO: Deep Analyisis
            //TODO: List all the names, ips, accounts instead of showing them as individual embeds.
            await ctx.ReplyWorkingAsync();
            var tFetchAsync1 = Starwatch.GetSessionsAsync(character: name);
            var tFetchAsync2 = Starwatch.GetSessionsAsync(account: name);
            var responses = await Task.WhenAll(tFetchAsync1, tFetchAsync2);

            bool hasGroupA = responses[0].Payload.Length > 0;
            Session[] groupA = responses[hasGroupA ? 0 : 1].Payload;
            Session[] groupB = responses[hasGroupA ? 1 : 0].Payload;

            if (groupA.Length == 0)
            {
                await ctx.ReplyAsync("_No Results_");
                return;
            }

            //IEnumerable<Session> sessions;
            //if (groupA.Length > 0) sessions = groupA.Union(groupB, LambdaEqualityComparer<Session>.Create((a, b) => a.Id == b.Id, (a) => a.Id.GetHashCode()));
            //else sessions = groupB;
            var pages = new Page[]
            {
                new Page("**Characters**\n```\n" + string.Join("\n", groupA.Select(s => s.Username).Union(groupB.Select(s => s.Username)).Where(s => !string.IsNullOrEmpty(s)).ToHashSet().ToList())  + "\n```"),
                new Page("**Accounts**\n```\n" + string.Join("\n", groupA.Select(s => s.Account).Union(groupB.Select(s => s.Account)).Where(s => !string.IsNullOrEmpty(s)).ToHashSet().ToList()) + "\n```"),
                new Page("**UUIDs**\n```\n" + string.Join("\n", groupA.Select(s => s.UUID).Union(groupB.Select(s => s.UUID)).Where(s => !string.IsNullOrEmpty(s)).ToHashSet().ToList()) + "\n```"),
                new Page("**IPs**\n```\n" + string.Join("\n", groupA.Select(s => s.IP).Union(groupB.Select(s => s.IP)).Where(s => !string.IsNullOrEmpty(s)).ToHashSet().ToList()) + "\n```"),
            };

            string content = string.Join('\n', pages.Select(p => p.Content));
            await ctx.ReplyAsync(content);
        }

        [Command("search")]
        [Description("Searches sessions")]
        [Permission("sw.search")]
        public async Task SearchSessions(CommandContext ctx, [RemainingText] CommandQuery query)
        {
            string account      = query.GetString("account", null);
            string character    = query.GetString("character", null);
            string uuid         = query.GetString("uuid", null);
            string ip           = query.GetString("ip", null);
            await SearchSessions(ctx, account, character, ip, uuid);
        }

        [Command("search")]
        public async Task SearchSessions(CommandContext ctx,
            [Description("Account name to search")]     string account = null,
            [Description("Character name to search")]   string character = null,
            [Description("IP address to search")]       string ip = null,
            [Description("UUID to search")]             string uuid = null)
        {
            await ctx.ReplyWorkingAsync();
            var response = await Starwatch.GetSessionsAsync(account, character, ip, uuid);
            var lines = new string[] {
                "**Characters**\n```\n" + string.Join("\n", response.Payload.Select(s => s.Username).Where(s => !string.IsNullOrEmpty(s)).ToHashSet()) + "\n```",
                "**Accounts**\n```\n" + string.Join("\n", response.Payload.Select(s => s.Account).Where(s => !string.IsNullOrEmpty(s)).ToHashSet()) + "\n```",
                "**UUIDs**\n```\n" + string.Join("\n", response.Payload.Select(s => s.UUID).Where(s => !string.IsNullOrEmpty(s)).ToHashSet()) + "\n```",
                "**IPs**\n```\n" + string.Join("\n",response.Payload.Select(s => s.IP).Where(s => !string.IsNullOrEmpty(s)).ToHashSet()) + "\n```",
            };

            await ctx.ReplyAsync(string.Join('\n', lines));
        }


        [Command("endpoint")]
        [Permission("sw.endpoint")]
        [RequireOwner]
        public async Task ExecuteEndpoint(CommandContext ctx, [RemainingText] string endpoint)
        {
            await ctx.ReplyWorkingAsync();
            var response = await Starwatch.GetRequestAsync(endpoint);
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(response, Newtonsoft.Json.Formatting.Indented);
            await ctx.ReplyAsync("```json\n" + json + "\n```");
        }

        [Command("tagged")]
        [Permission("sw.tagged")]
        [Description("Breaks down a message tags.")]
        public async Task TagText(CommandContext ctx, [RemainingText] string text)
        {
            TaggedText tt = new TaggedText(text);
            await ctx.ReplyAsync("**Content Tagged:**\n```\n" + tt.Content + "\n```\n**Breakdown:**\n```\n" + string.Join('\n', tt.Tags.Select(t => t.color + ": " + t.text + "")) + "\n```");
        }

        [Command("upload")]
        [Description("Uploads a world.")]
        [RequireOwner]
        public async Task UploadWorld(CommandContext ctx, string world)
        {
            if (ctx.Message.Attachments.Count == 0)
                throw new ArgumentNullException("Attachments", "The world must have an attachment");

            if (string.IsNullOrWhiteSpace(world))
                throw new ArgumentNullException("world");

            //We are working
            await ctx.TriggerTypingAsync();

            //Download the file, then upload it again
            var url = ctx.Message.Attachments[0].Url;
            using (var client = new System.Net.WebClient())
            {
                string tmpname = Path.GetTempFileName();
                try
                {
                    //Throws unauth
                    await client.DownloadFileTaskAsync(new Uri(url), tmpname);
                    byte[] response = await client.UploadFileTaskAsync(new Uri($"http://localhost:8000/world/{world}"), tmpname);
                    await ctx.ReplyAsync(content: Encoding.UTF8.GetString(response));
                }
                catch(Exception e)
                {
                    await ctx.ReplyExceptionAsync(e);
                }
                finally
                {
                    if (File.Exists(tmpname))
                        File.Delete(tmpname);
                }

            }
        }
    }
}
