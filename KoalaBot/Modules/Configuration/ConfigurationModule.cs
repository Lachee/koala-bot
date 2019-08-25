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
using DSharpPlus.Entities;
using KoalaBot.CommandNext;

namespace KoalaBot.Modules.Configuration
{
    [Group("config")]
    public partial class ConfigurationModule : BaseCommandModule
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
        [Permission("koala.config.prefix")]
        public async Task SetPrefix(CommandContext ctx, string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentNullException("prefix", "Prefix cannot be null or empty.");

            //Fetch the settings, update its prefix then save again
            var settings = await GuildSettings.GetGuildSettingsAsync(ctx.Guild);
            settings.Prefix = prefix;
            await settings.SaveAsync();

            //Respond that we did that.
            await ctx.ReplyReactionAsync(true);
        }

        [Command("blackbacon"), Aliases("bb")]
        [Description("Sets the black bacon")]
        [Permission("koala.config.bb")]
        public async Task SetBlackBacon(CommandContext ctx, DiscordRole role)
        {
            if (role == null)
                throw new ArgumentNullException("role", "Role cannot be null!");

            //Fetch the settings, update its prefix then save again
            var settings = await GuildSettings.GetGuildSettingsAsync(ctx.Guild);
            settings.BlackBaconId = role.Id;
            await settings.SaveAsync();

            //Respond that we did that.
            await ctx.ReplyReactionAsync(true);
        }


        [Command("modlog"), Aliases("log")]
        [Description("Sets the mod log channel")]
        [Permission("koala.config.modlog")]
        public async Task SetBlackBacon(CommandContext ctx, DiscordChannel channel)
        {
            //Fetch the settings, update its prefix then save again
            var settings = await GuildSettings.GetGuildSettingsAsync(ctx.Guild);
            settings.ModLogId = (channel?.Id).GetValueOrDefault(0);
            await settings.SaveAsync();

            //Respond that we did that.
            await ctx.ReplyReactionAsync(true);
        }

        [Command("sync_tally")]
        [RequireOwner]
        [Hidden]
        public async Task SyncTallies(CommandContext ctx)
        {
            await Bot.MessageCounter.SyncChanges();
            await ctx.ReplyReactionAsync(true);
        }
    }
}
