using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using KoalaBot.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace KoalaBot.Entities
{
    public class ModWatcher
    {
        public Koala Bot { get; }
        public IRedisClient Redis => Bot.Redis;

        public ModWatcher(Koala bot)
        {
            this.Bot = bot;

            bot.Discord.GuildMemberAdded += async (evt) =>
            {
                //Try to enforce the nickname of new members. They may have left to bypass the enforcement
                await TryEnforceNickname(evt.Member);
            };

            bot.Discord.GuildMemberUpdated += async (evt) =>
            {
                if (evt.Member.IsBot) return;

                //If the nickname has changed, enforce it again
                if (evt.NicknameAfter != evt.NicknameBefore)
                    await TryEnforceNickname(evt.Member);

                //The roles have changed, enforce it again
                if (evt.RolesAfter != evt.RolesBefore)
                    await HandleRoleChange(evt);
            };
        }

        private async Task<bool> TryEnforceNickname(DiscordMember member)
        {
            var enforcedNickname = await Redis.FetchStringAsync(Namespace.Combine(member.Guild, member, "nickname"), EnforceNicknameActivity.KEY_NICKNAME, @default: null);
            if (enforcedNickname != null && member.Nickname != enforcedNickname)
            {
                await member.ModifyAsync(model => { model.Nickname = enforcedNickname; model.AuditLogReason = "Nickname Enforcemnet"; });
                return true;
            }

            return false;
        }


        private async Task HandleRoleChange(GuildMemberUpdateEventArgs args)
        {
        }
    }
}