using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using KoalaBot.Extensions;
using KoalaBot.Logging;
using KoalaBot.Managers;
using KoalaBot.Permissions.CommandNext;
using KoalaBot.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Modules.Configuration
{
    public partial class ConfigurationModule
    {
        [Group("react"), Aliases("rr", "reactroll")]
        [Description("Handles role awarding from reactions")]
        public partial class ReactRoleModule : BaseCommandModule
        {
            public Koala Bot { get; }
            public IRedisClient Redis => Bot.Redis;
            public Logger Logger { get; }
            public ReactRoleManager Manager => Bot.ReactRoleManager;

            public ReactRoleModule(Koala bot)
            {
                this.Bot = bot;
                this.Logger = new Logger("CMD-ROLE", bot.Logger);
            }

            [Command("add"), Aliases("+", "a", "link")]
            [Description("Adds a connection between a reaction and an emoji on a message.")]
            [Permission("koala.config.reactrole.add")]
            public async Task LinkRoleEmojiAsync(CommandContext ctx, DiscordMessage message, DiscordEmoji emoji, DiscordRole role)
            {
                await Manager.AddReactionRoleAsync(message, emoji, role);
                await ctx.ReplyReactionAsync(true);
                await message.CreateReactionAsync(emoji);
            }

            [Command("remove"), Aliases("-", "r", "unlink")]
            [Description("Removes a connection between a reaction and an emoji on a message.")]
            [Permission("koala.config.reactrole.remove")]
            public async Task UnlinkRoleEmojiAsync(CommandContext ctx, DiscordMessage message, DiscordEmoji emoji)
            {
                await Manager.RemoveReactionRoleAsync(message, emoji);
                await ctx.ReplyReactionAsync(true);
                await message.DeleteOwnReactionAsync(emoji);
            }
        }
    }
}
