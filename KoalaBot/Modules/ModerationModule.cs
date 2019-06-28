using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using KoalaBot.Redis;
using KoalaBot.Extensions;
using KoalaBot.Logging;
using DSharpPlus.Entities;
using KoalaBot.Entities;
using KoalaBot.Exceptions;
using System.Linq;
using KoalaBot.Permissions.CommandNext;

namespace KoalaBot.Modules
{
    [Group("mod"), RequirePermissions(DSharpPlus.Permissions.ManageMessages)]
    public class ModerationModule : BaseCommandModule
    {
        public Koala Bot { get; }
        public IRedisClient Redis => Bot.Redis;
        public ModerationManager ModerationManager => Bot.ModerationManager;
        public Logger Logger { get; }

        public ModerationModule(Koala bot)
        {
            this.Bot = bot;
            this.Logger = new Logger("CMD-MOD", bot.Logger);
        }

        #region Utility
        [Command("cleanup"), Aliases("clean", "c")]
        [Description("Deletes all bot messages from the channel.")]
        [Permission("koala.mod.cleanup")]
        public async Task Cleanup(CommandContext ctx, [Description("Number of messages to delete")] int count = 10)
        {
            await Bot.ReplyManager.DeleteResponsesAsync(ctx.Channel, count);
            await ctx.ReplyReactionAsync(true);
        }

        [Command("roles"), Hidden]
        [Permission("koala.mod.roles")]
        public async Task Role (CommandContext ctx)
        {
            string roles = string.Join("\n", ctx.Member.Roles.Select(r => r.Id + "\t" + r.Name));
            await ctx.RespondAsync("```\n" + roles + "```");
        }
        #endregion
        
        #region Nickname
        [Command("nickname"), Aliases("nick", "name")]
        [Description("Enforces the nickname of a user.")]
        [Permission("koala.mod.nickname", restrictChannel: false)]
        public async Task ForceNickname(CommandContext ctx, 
            [Description("The member to enforce the nickname onto.")]                                       DiscordMember member, 
            [Description("The nickname to enforce. Leave blank to remove enforcement."), RemainingText]     string nickname = null)
        {
            if (member == null)
                throw new ArgumentNullException("member");

            if (member == ctx.Member)
                throw new ArgumentException("Cannot be yourself", "member");

            var success = await ModerationManager.SetNicknameEnforcementAsync(member, nickname, moderator: ctx.Member);
            await ctx.ReplyReactionAsync(success);
        }

        [Command("nickname")]
        [Description("Enforces the nickname of a user.")]
        [Permission("koala.mod.nickname", restrictChannel: false)]
        public async Task ForceNickname(CommandContext ctx,
            [Description("The member to enforce the nickname onto.")]                                       DiscordMember member,
            [Description("The duration of the nickname to enforce.")]                                       TimeSpan duration,
            [Description("The nickname to enforce. Leave blank to remove enforcement."), RemainingText]     string nickname = null)
        {
            if (member == null)
                throw new ArgumentNullException("member");

            if (member == ctx.Member)
                throw new ArgumentException("Cannot be yourself", "member");

            var success = await ModerationManager.SetNicknameEnforcementAsync(member, nickname, duration: duration, moderator: ctx.Member);
            await ctx.ReplyReactionAsync(success);
        }
        #endregion

        #region Black Bacon
        [Command("blackbacon"), Aliases("bb", "globalmute", "gm")]
        [Description("Mutes a member form the server with Black Bacon")]
        [Permission("koala.mod.bb", restrictChannel: false)]
        public async Task BlackBacon(CommandContext ctx, DiscordMember member, [RemainingText] string reason)
        {
            if (member == null)
                throw new ArgumentNullException("member");

            if (member == ctx.Member)
                throw new ArgumentException("Cannot be yourself", "member");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentNullException("Reason cannot be empty.");

            await ModerationManager.GiveBlackBaconAsync(member, ctx.Member, reason);
        }

        [Command("unblackbacon"), Aliases("ubb", "unbb", "unglobalmute", "ungm")]
        [Description("Removes a black bacon mute for a member")]
        [Permission("koala.mod.bb", restrictChannel: false)]
        public async Task UnblackBacon(CommandContext ctx, DiscordMember member)
        {
            if (member == null)
                throw new ArgumentNullException("member");

            if (member == ctx.Member)
                throw new ArgumentException("Cannot be yourself", "member");

            var settings = await GuildSettings.GetGuildSettingsAsync(ctx.Guild);
            if (settings.GuildId < 0)
                throw new Exception("Black Bacon role not yet defined.");

            var bbrole = settings.GetBlackBaconRole();
            if (bbrole == null)
                throw new Exception("Black Bacon role not yet defined or has since been deleted.");

            //Get all the roles and clear out the store
            var rStoreKey = Namespace.Combine(ctx.Guild, member, "bbroles");
            var rStore = await Bot.Redis.FetchHashSetAsync(rStoreKey);
            await Bot.Redis.RemoveAsync(rStoreKey);

            if (rStore == null)
            {
                await ctx.ReplyReactionAsync(false);
                return;
            }

            //Prepare the roles
            var roles = rStore
                .Select(str => ulong.TryParse(str, out var id) ? id : 0)
                .Select(id => ctx.Guild.GetRole(id))
                .Where(role => role != null);

            //Award the roles
            await member.ReplaceRolesAsync(roles, ctx.Member.Username + " removed BB");

            //Apply
            await Bot.PermissionManager.ApplyRolesAsync(member);

            //Alert
            await ctx.ReplyAsync($"{member.DisplayName} was un-black baconed.");
            await Bot.ModerationManager.RecordModerationAsync(ctx, member, "Unblack Baconed");
        }
        #endregion

        [Command("silence"), Aliases("mute")]
        [Description("Silences a member form a channel")]
        [Permission("koala.mod.silence", restrictChannel: true)]
        public async Task Silence(CommandContext ctx, DiscordMember member, [RemainingText] string reason)
        {
            if (member == null)
                throw new ArgumentNullException("member");

            if (member == ctx.Member)
                throw new ArgumentException("Cannot be yourself", "member");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentNullException("reason");

            //Add the override & add the mod log
            var overwrite = ctx.Channel.PermissionOverwrites.FirstOrDefault(o => o.Id == member.Id && o.Type == DSharpPlus.OverwriteType.Member);
            await ctx.Channel.AddOverwriteAsync(member, overwrite.Allowed, overwrite.Denied | DSharpPlus.Permissions.SendMessages | DSharpPlus.Permissions.AddReactions);

            //TODO: Report reasoning on MySQL
            await ctx.ReplyAsync($"{member.DisplayName} was silenced from this channel: ```{reason}```");
            await Bot.ModerationManager.RecordModerationAsync(ctx, member, "Silenced: " + ctx.Channel.Mention + ". " + reason);
        }

        [Command("unsilence"), Aliases("unmute")]
        [Description("unsilences a member form a channel")]
        [Permission("koala.mod.silence", restrictChannel: true)]
        public async Task Unsilence(CommandContext ctx, DiscordMember member)
        {
            if (member == null)
                throw new ArgumentNullException("member");

            if (member == ctx.Member)
                throw new ArgumentException("Cannot be yourself", "member");

            //Add the override & add the mod log
            var overwrite = ctx.Channel.PermissionOverwrites.FirstOrDefault(o => o.Id == member.Id && o.Type == DSharpPlus.OverwriteType.Member);
            await ctx.Channel.AddOverwriteAsync(member, overwrite.Allowed, overwrite.Denied & ~(DSharpPlus.Permissions.SendMessages | DSharpPlus.Permissions.AddReactions));

            //TODO: Report reasoning on MySQL
            await ctx.ReplyAsync($"{member.DisplayName} was unsilenced from this channel.");
            await Bot.ModerationManager.RecordModerationAsync(ctx, member, "Unsilenced: " + ctx.Channel.Mention);
        }

        [Command("delete"), Aliases("d", "remove")]
        [Description("Deletes a message from the channel")]
        [Permission("koala.mod.delete")]
        public async Task DeleteMessage(CommandContext ctx, DiscordMessage message, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentNullException("reason");

            await ctx.Channel.DeleteMessagesAsync(new DiscordMessage[] { message, ctx.Message }, ctx.Member.Username + " delete");
            await Bot.ModerationManager.RecordModerationAsync(ctx, message.Author, reason);
        }

        [Command("pin"), Aliases("p")]
        [Description("Pins a message to the channel")]
        [Permission("koala.mod.pin.add")]
        public async Task PinMessage(CommandContext ctx, DiscordMessage message)
        {
            await message.PinAsync();
            await ctx.ReplyReactionAsync(true);
        }

        [Command("unpin"), Aliases("up")]
        [Description("Unpins a message from the channel")]
        [Permission("koala.mod.pin.remove")]
        public async Task UnpinMessage(CommandContext ctx, DiscordMessage message)
        {
            await message.UnpinAsync();
            await ctx.ReplyReactionAsync(true);
        }
    }
}
