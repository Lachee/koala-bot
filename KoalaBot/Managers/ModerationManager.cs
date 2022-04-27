using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using KoalaBot.Database;
using KoalaBot.Entities;
using KoalaBot.Extensions;
using KoalaBot.Logging;
using KoalaBot.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace KoalaBot.Managers
{
    public class ModerationManager : Manager
    {
        //Note: these are not guild prefixed. They really should be.
        private HashSet<ulong> _handledBans = new HashSet<ulong>();

        public ModerationManager(Koala bot, Logger logger = null) : base(bot, logger)
        {

            bot.Discord.GuildMemberAdded += async (client, evt) =>
            {
                //Try to enforce the nickname of new members. They may have left to bypass the enforcement
                await TryEnforceNickname(evt.Member);
                await TryEnforceBlackBacon(evt.Member);
            };

            bot.Discord.GuildMemberUpdated += async (client, evt) =>
            {
                if (evt.Member.IsBot) return;

                //If the nickname has changed, enforce it again
                if (evt.NicknameAfter != evt.NicknameBefore)
                    await TryEnforceNickname(evt.Member);

                //The roles have changed, enforce it again
                if (evt.RolesAfter.Count != evt.RolesBefore.Count || evt.RolesAfter.Intersect(evt.RolesBefore).Count() != evt.RolesAfter.Count)
                {
                   // await TryEnforceBlackBacon(evt.Member);
                    if (!await TryEnforceBlackBacon(evt.Member))
                        await Bot.PermissionManager.ApplyRolesAsync(evt.Member);
                }
            };

            bot.Discord.GuildBanRemoved += async (client, evt) =>
            {
                _handledBans.Remove(evt.Member.Id);
            };

            bot.Discord.GuildBanAdded += async (client, evt) =>
            {
                if (!_handledBans.Remove(evt.Member.Id))
                {
                    var logs = await evt.Guild.GetAuditLogsAsync(action_type: AuditLogActionType.Ban);
                    var log = logs.Select(l => l as DiscordAuditLogBanEntry).Where(l => l?.Target == evt.Member).FirstOrDefault();
                    if (log != null)
                    {
                        //Record the ban
                        await RecordModerationAsync("ban", evt.Guild, evt.Member, moderator: log.UserResponsible, reason: log.Reason);
                        if (!log.UserResponsible.IsBot)
                        {
                            var member = await evt.Guild.GetMemberAsync(log.UserResponsible);
                            await member.SendMessageAsync("**Moderation Failure**\nYou have moderated the server " + evt.Guild.Name + " without using me. Please use the `mod` commands next time.");
                        }
                    }
                    else
                    {
                        //WE have no reason why they were banned, so we will jsut guess
                        await RecordModerationAsync("ban", evt.Guild, evt.Member, reason: "Unkown Reason.");
                    }
                }
            };
        }

        /// <summary>
        /// Kicks a member from the server
        /// </summary>
        /// <param name="member"></param>
        /// <param name="moderator"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task KickMemberAsync(DiscordMember member, DiscordMember moderator = null, string reason = null)
        {
            await member.RemoveAsync();
            await RecordModerationAsync("kick", moderator, member, reason);
        }

        public async Task BanMemberAsync(DiscordMember member, int deleteMessages = 0, DiscordMember moderator = null, string reason = null)
        {
            _handledBans.Add(member.Id);
            await member.BanAsync(deleteMessages, reason);
            await RecordModerationAsync("ban", moderator, member, reason);
        }

        /// <summary>
        /// Records a moderative command
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="subject"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task<ModerationLog> RecordModerationAsync(CommandContext ctx, DiscordUser subject, string reason) => await RecordModerationAsync(ctx.Command.Name, ctx.Member, subject, reason);
        
        
        /// <summary>
        /// Records a moderative action
        /// </summary>
        /// <param name="action"></param>
        /// <param name="moderator"></param>
        /// <param name="subject"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task<ModerationLog> RecordModerationAsync(string action, DiscordMember moderator, DiscordUser subject, string reason)
        {

            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentNullException("action");

            if (moderator == null)
                throw new ArgumentNullException("moderator");

            //Get the moderation log and save it
            ModerationLog log = new ModerationLog(action, moderator, subject, reason);
            await log.SaveAsync(DbContext);

            //Report the log
            await ReportModerationLogAsync(log, moderator.Guild, subject: subject, moderator: moderator);

            return log;
        }

        public async Task<ModerationLog> RecordModerationAsync(string action, DiscordGuild guild, DiscordUser subject, DiscordUser moderator = null,  string reason = null)
        {
            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentNullException("action");

            ModerationLog log = new ModerationLog(action, guild, subject, moderator, reason);
            await log.SaveAsync(DbContext);
            await ReportModerationLogAsync(log, guild, subject: subject, moderator: moderator);
            return log;
        }

        private async Task ReportModerationLogAsync(ModerationLog log, DiscordGuild guild = null, DiscordUser subject = null, DiscordUser moderator = null)
        {
            if (guild == null)
                guild = await Bot.Discord.GetGuildAsync(log.GuildId);

            if (moderator == null && log.ModeratorId > 0)
                moderator = await Bot.Discord.GetUserAsync(log.ModeratorId);

            if (subject == null && log.SubjectId > 0)
                subject = await Bot.Discord.GetUserAsync(log.SubjectId);

            //Figure out what hte guild modlog folder is
            var gs = await GuildSettings.GetGuildSettingsAsync(guild);
            if (gs.ModLogId > 0)
            {
                StringBuilder content = new StringBuilder();
                content.AppendFormat($"**{log.Action}** | {log.Id}\n");

                if (subject != null)
                    content.AppendLine($"**User** : {subject.Mention}  ( {subject.Id} )");

                if (moderator != null)
                    content.AppendLine($"**Moderator** : {moderator.Mention}  ( {moderator.Id} )");

                if (!string.IsNullOrWhiteSpace(log.Reason))
                    content.AppendLine($"**Reason**: ```{log.Reason}```");

                //Send the message and update our log
                var msg = await gs.GetModLogChannel().SendMessageAsync(content.ToString());
                log.Message = msg.Id;

                //Save the log
                await log.SaveAsync(DbContext);
            }
        }

        /// <summary>Tries to enforce the nickname of a user</summary>
        public async Task<bool> TryEnforceNickname(DiscordMember member)
        {
            var enforcedNickname = await Redis.FetchStringAsync(Namespace.Combine(member.Guild, member, "nickname"), @default: null);
            if (enforcedNickname != null && !member.Nickname.Equals(enforcedNickname, StringComparison.InvariantCultureIgnoreCase))
            {
                try
                {
                    //Enforce the nickname
                    Logger.Log("Enforcing {0} Nickname.", member);
                    await member.ModifyAsync(model => { model.Nickname = enforcedNickname; model.AuditLogReason = "Nickname Enforcemnet"; });
                }
                catch (DSharpPlus.Exceptions.RateLimitException)
                {
                    //The have triggered the ratelimit, probably abusing something. Kick em for saftey.
                    Logger.Log("Kicking {0} because hit ratelimit while trying to enforce it.", member);
                    await KickMemberAsync(member, reason: "Nickname Enforcement hit ratelimit while trying to enforce it.");
                }

                return true;
            }

            return false;
        }

        /// <summary>Tries to enforce the roles of a user. Returns true if the user is being BB.</summary>
        public async Task<bool> TryEnforceBlackBacon(DiscordMember member)
        {
            //Make sure the user is actually muted
            if (!await Redis.ExistsAsync(Namespace.Combine(member.Guild, member, "bbroles")))
                return false;

            //Get the settings
            var settings = await member.Guild.GetSettingsAsync();
            var bbrole = settings.GetBlackBaconRole();
            if (bbrole == null) return false;

            //Remute the player
            if (member.Roles.Count() != 1 || !member.Roles.Contains(bbrole))
            {
                try
                {
                    Logger.Log("Enforcing Black Bacon");
                    await member.ReplaceRolesAsync(new DiscordRole[] { bbrole });
                }
                catch (DSharpPlus.Exceptions.RateLimitException)
                {
                    //The have triggered the ratelimit, probably abusing something. Kick em for saftey.
                    Logger.Log("Kicking {0} because hit ratelimit while trying to enforce it.", member);
                    await KickMemberAsync(member, reason: "Black Bacon Enforcement hit ratelimit while trying to enforce it.");
                }
                return true;
            }

            return true;
        }

        /// <summary>
        /// Sets the enforcement of a user nickname
        /// </summary>
        /// <param name="member">The member to enforce</param>
        /// <param name="nickname">The nickname to enforce</param>
        /// <returns></returns>
        public async Task<bool> SetNicknameEnforcementAsync(DiscordMember member, string nickname, TimeSpan? duration = null, DiscordMember moderator = null)
        {
            if (member == null)
                throw new ArgumentNullException("member");


            string nicknameKey = Namespace.Combine(member.Guild, member, "nickname");

            if (string.IsNullOrWhiteSpace(nickname))
            {
                //Remove the enforcement
                bool success = await Redis.RemoveAsync(nicknameKey);
                if (success) 
                {
                    await member.ModifyAsync((m) => { m.Nickname = null; m.AuditLogReason = "Enforcement Removed"; });
                    await Bot.ModerationManager.RecordModerationAsync("nickname", moderator, member, "Enforcement Removed");
                }

                return success;
            }
            else
            {
                //Add the enforcement and then trigger it
                await Redis.StoreStringAsync(nicknameKey, nickname, duration);
                await member.ModifyAsync((m) => { m.Nickname = nickname; m.AuditLogReason = "Enforcement Created"; });
                await Bot.ModerationManager.RecordModerationAsync("nickname", moderator, member, "Enforced to " + nickname);
                return true;
            }
        }

        /// <summary>
        /// Applies the black bacon role to a user.
        /// </summary>
        /// <param name="member"></param>
        /// <param name="moderator"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task GiveBlackBaconAsync(DiscordMember member, DiscordMember moderator = null, string reason = null)
        {

            //Get the guild settings, and make sure they are valid
            var settings = await GuildSettings.GetGuildSettingsAsync(member.Guild);
            if (settings.GuildId < 0)
                throw new Exception("Black Bacon role not yet defined.");

            var bbrole = settings.GetBlackBaconRole();
            if (bbrole == null)
                throw new Exception("Black Bacon role not yet defined or has since been deleted.");

            //Get the keys
            var rStoreKey = Namespace.Combine(member.Guild, member, "bbroles");
            if (await Redis.ExistsAsync(rStoreKey))
                throw new Exception("The user has already been black baconed.");

            //Calculate what roles we should add to the list
            var rStore = member.Roles
                            .Prepend(member.Guild.EveryoneRole)
                            .Where(r => r.Id != bbrole.Id)
                            .Select(r => r.Id.ToString())
                            .ToHashSet();

            //Add the hash, apply the roles and update their sync
            await Bot.Redis.AddHashSetAsync(rStoreKey, rStore);
            await member.ReplaceRolesAsync(new DiscordRole[] { bbrole });
            await Bot.ModerationManager.RecordModerationAsync("blackbacon", moderator, member, reason);
        }
        
        /// <summary>
        /// Removes the black bacon role to a user.
        /// </summary>
        /// <param name="member"></param>
        /// <param name="moderator"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task<bool> RemoveBlackBaconAsync(DiscordMember member, DiscordMember moderator = null)
        {

            //Get the guild settings, and make sure they are valid
            var settings = await GuildSettings.GetGuildSettingsAsync(member.Guild);
            if (settings.GuildId < 0)
                throw new Exception("Black Bacon role not yet defined.");

            var bbrole = settings.GetBlackBaconRole();
            if (bbrole == null)
                throw new Exception("Black Bacon role not yet defined or has since been deleted.");

            //Get all the roles and clear out the store
            var rStoreKey = Namespace.Combine(member.Guild, member, "bbroles");
            var rStore = await Bot.Redis.FetchHashSetAsync(rStoreKey);
            await Bot.Redis.RemoveAsync(rStoreKey);

            if (rStore == null)
                return false;
            
            //Prepare the roles
            var roles = rStore
                .Select(str => ulong.TryParse(str, out var id) ? id : 0)
                .Select(id => member.Guild.GetRole(id))
                .Where(role => role != null);

            //Award the roles
            await member.ReplaceRolesAsync(roles, member.Username + " removed BB");
            await Bot.ModerationManager.RecordModerationAsync("blackbacon", moderator, member, "Black Bacon Removed");
            return true;
        }
    }
}