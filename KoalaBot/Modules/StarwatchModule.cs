using System;
using System.Collections.Generic;
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

namespace KoalaBot.Modules
{
    [Group("starwatch"), Aliases("sw", "starw", "swatch", "s")]
    public class StarwatchModule : BaseCommandModule
    {
        public Koala Bot { get; }
        public IRedisClient Redis => Bot.Redis;
        public Logger Logger { get; }

        public StarwatchModule(Koala bot)
        {
            this.Bot = bot;
            this.Logger = new Logger("CMD-SW", bot.Logger);
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
