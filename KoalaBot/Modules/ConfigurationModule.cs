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

namespace KoalaBot.Modules
{
    [Group("config")]
    public class ConfigurationModule : BaseCommandModule
    {
        public Koala Bot { get; }
        public IRedisClient Redis => Bot.Redis;
        public Logger Logger { get; }

        public ConfigurationModule(Koala bot)
        {
            this.Bot = bot;
            this.Logger = new Logger("CMD-CONFIG", bot.Logger);
        }

        [Command("prefix")]
        [Description("Sets the prefix of the bot for the guild.")]
        [RequirePermissions(DSharpPlus.Permissions.ManageGuild)]
        public async Task SetPrefix(CommandContext ctx, string prefix)
        {
            await Bot.UpdatePrefix(ctx.Guild, prefix);
            await ctx.RespondReactionAsync(true);
        }

        [Command("sync_tally")]
        [RequireOwner]
        [Hidden]
        public async Task SyncTallies(CommandContext ctx)
        {
            await Bot.MessageCounter.SyncChanges();
            await ctx.RespondReactionAsync(true);
        }

        [Command("avatar")]
        [RequireOwner]
        [Hidden]
        public async Task SetAvatar(CommandContext ctx, [RemainingText] string avatar = null)
        {
            await ctx.TriggerTypingAsync();

            if (!string.IsNullOrWhiteSpace(avatar))
            {
                avatar = Bot.Configuration.Resources + "avatar\\" + avatar;
                Logger.Log("Setting avatar to " + avatar);
            }
            else
            {
                //Get all files
                string[] files = Directory.GetFiles(Bot.Configuration.Resources + "avatar\\", "*.png");

                //Pick a random one
                Random random = new Random();
                avatar = files[random.Next(files.Length)];
                Logger.Log("Setting random avatar to " + avatar);
            }


            //Set it
            using (FileStream stream = new FileStream(avatar, FileMode.Open, FileAccess.Read))
                await ctx.Client.UpdateCurrentUserAsync(avatar: stream);

            //Respond
            await ctx.RespondReactionAsync(true);
            await ctx.RespondWithFileAsync(avatar, "Wenk");
        }

    }
}
