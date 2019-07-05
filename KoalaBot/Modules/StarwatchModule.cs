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
using KoalaBot.Permissions.CommandNext;
using DSharpPlus.Entities;
using KoalaBot.Starwatch;
using KoalaBot.CommandNext;
using KoalaBot.Starwatch.Entities;
using DSharpPlus.Interactivity;
using KoalaBot.Util;

namespace KoalaBot.Modules
{
    [Group("starwatch"), Aliases("sw", "starw", "swatch", "s")]
    [RequireOwner]
    public class StarwatchModule : BaseCommandModule
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
        
        [Command("endpoint")]
        [Permission("sw.endpoint")]
        public async Task ExecuteEndpoint(CommandContext ctx, [RemainingText] string endpoint)
        {
            var response = await Starwatch.GetRequestAsync(endpoint);
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(response, Newtonsoft.Json.Formatting.Indented);
            await ctx.ReplyAsync("```json\n" + json + "\n```");
        }

        [Command("aliases")]
        [Description("Gets the aliases of a user")]
        [Permission("sw.alias")]
        public async Task SearchAliasSessions(CommandContext ctx, string name)
        {
            await ctx.TriggerTypingAsync();
            var tFetchAsync1 = Starwatch.GetSessionsAsync(character: name);
            var tFetchAsync2 = Starwatch.GetSessionsAsync(account: name);
            var responses = await Task.WhenAll(tFetchAsync1, tFetchAsync2);

            IEnumerable<Session> sessions;
            Session[] groupA = responses[0].Object;
            Session[] groupB = responses[1].Object;

            if (groupA.Length > 0) sessions = groupA.Union(groupB, LambdaEqualityComparer<Session>.Create((a, b) => a.Id == b.Id, (a) => a.Id.GetHashCode()));
            else sessions = groupB;
            
            var pages = sessions.Select(s => new Page(embed: s.GetEmbedBuilder()));
            await Bot.Interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.Member, pages);
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
            var response = await Starwatch.GetSessionsAsync(account, character, ip, uuid);
            await ctx.ReplyAsync(response.Object.Length + " Results");
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
