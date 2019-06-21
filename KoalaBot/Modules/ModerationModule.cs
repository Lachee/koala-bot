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

namespace KoalaBot.Modules
{
    [Group("mod"), RequirePermissions(DSharpPlus.Permissions.ManageMessages)]
    public class ModerationModule : BaseCommandModule
    {
        public Koala Bot { get; }
        public IRedisClient Redis => Bot.Redis;
        public Logger Logger { get; }

        public ModerationModule(Koala bot)
        {
            this.Bot = bot;
            this.Logger = new Logger("CMD-MOD", bot.Logger);
        }

        [Command("roles")]
        public async Task Role (CommandContext ctx)
        {
            string roles = string.Join("\n", ctx.Member.Roles.Select(r => r.Id + "\t" + r.Name));
            await ctx.RespondAsync("```\n" + roles + "```");
        }

        [Command("nickname")]
        [Aliases("nick", "name")]
        [Description("Enforces the nickname of a user.")]
        public async Task ForceNickname(CommandContext ctx, 
            [Description("The member to enforce the nickname onto.")]                       DiscordMember member, 
            [Description("The nickname to enforce. Leave blank to remove enforcement.")]    string nickname = null)
        {
            if (member == null)
                throw new ArgumentNullException("member");

            if (member == ctx.Member && member.IsOwner)
                throw new ArgumentException("Cannot be yourself", "member");

            //Get any previous nickname and make sure we are allowed to do it
            var prevEnforcement = await Redis.FetchObjectAsync<EnforceNicknameActivity>(Namespace.Combine(ctx.Guild, member, "nickname"));
            if (prevEnforcement != null)
            {
                DiscordMember prevMember = await ctx.Guild.GetMemberAsync(prevEnforcement.ModeratorId);
                if (member.Hierarchy < prevMember.Hierarchy && prevMember != ctx.Member)
                    throw new PermissionException("Cannot modify enforcements created by users above you.");
            }

            //If we have a empty nickname then dump it, otherwise create a new one.
            if (string.IsNullOrWhiteSpace(nickname))
            {
                bool success = await Redis.RemoveAsync(Namespace.Combine(ctx.Guild, member, "nickname"));
                if (success) await member.ModifyAsync((m) => { m.Nickname = null; m.AuditLogReason = "Enforcement Removed"; });
                await ctx.RespondReactionAsync(success);
            }
            else
            {
                var activity = new EnforceNicknameActivity(nickname, ctx.Member, "");
                await Redis.StoreObjectAsync(Namespace.Combine(ctx.Guild, member, "nickname"), activity);
                await member.ModifyAsync((m) => { m.Nickname = nickname; m.AuditLogReason = "Enforcement Created"; });
                await ctx.RespondReactionAsync(true);
            }
        }
    }
}
