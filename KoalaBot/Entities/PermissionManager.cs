using DSharpPlus.Entities;
using KoalaBot.Logging;
using KoalaBot.Permissions;
using KoalaBot.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Entities
{
    public class PermissionManager
    {
        /*
            koala.ban                           Ban from server
            koala.kick                          Kick from server
            koala.silence                       Global role removal and mute

            koala.mute                          Mute from any channel
            koala.mute.389940452855382017       Mute from specific channel

            koala.see                           Allows role to see that channel

        (Everyone Role)
        group.364299133445734410:
            koala.see.389940452855382017

        (Admin Role)
        group.364297010112757781:
            group.364299133445734410
            ~koala.see.389940452855382017

        */

        public Koala Bot { get; }
        public Logger Logger { get; }
        public IRedisClient Redis => Bot.Redis;
        private Dictionary<ulong, GuildManager> _guilds;

        public PermissionManager(Koala bot, Logger logger = null)
        {
            Bot = bot;
            Logger = logger ?? new Logger("PERMS");
            _guilds = new Dictionary<ulong, GuildManager>();

            Bot.Discord.GuildAvailable += (args) =>
            {
                Logger.Log("Loading Guild {0}", args.Guild);
                var gm = new GuildManager(Bot, args.Guild, Logger.CreateChild(args.Guild.Name));
                gm.GroupSaved += OnGroupSaved;
                _guilds.Add(args.Guild.Id, gm);
                return Task.CompletedTask;
            };

            Bot.Discord.GuildUnavailable += (args) =>
            {
                Logger.Log("Clearing Guild {0}", args.Guild);
                _guilds.Remove(args.Guild.Id);
                return Task.CompletedTask;
            };

            Bot.Discord.GuildMemberRemoved += async (args) =>
            {
                Logger.Log("Deleting Member {0}", args.Member);
                var manager = GetGuildManager(args.Guild);
                var group = await manager.GetMemberGroupAsync(args.Member);
                await group.DeleteAsync();
            };

            Bot.Discord.GuildRoleDeleted += async (args) =>
            {
                Logger.Log("Deleting Role {0}", args.Role);
                var manager = GetGuildManager(args.Guild);
                var group = await manager.GetRoleGroupAsync(args.Role);
                await group.DeleteAsync();
            };
        }

        private async Task OnGroupSaved(Permissions.Events.GroupEventArgs e)
        {
            if (e.Group is MemberGroup mg)
                await this.ApplyRolesAsync(mg.Member);
        }

        public GuildManager GetGuildManager(DiscordGuild guild) => _guilds[guild.Id];
        public GuildManager GetGuildManager(DiscordMember member) => GetGuildManager(member.Guild);
        public Task<MemberGroup> GetMemberGroupAsync(DiscordMember member) => GetGuildManager(member).GetMemberGroupAsync(member);


        public async Task ApplyRolesAsync(DiscordMember member)
        {
            Logger.Log("Sync Roles for {0}", member);

            //Get all the role groups
            var mg = await GetMemberGroupAsync(member);
            var permissions = await mg.EvaluatePermissionChildrenAsync("group.role");

            //Custom collapse
            Dictionary<string, State> collapsed = new Dictionary<string, State>(permissions.Count);
            foreach(var p in permissions)
            {
                if (p.name.Length <= 11)
                {
                    //This is a universal modifier for all previous roles
                    foreach (var permname in collapsed.Keys.ToList())
                        collapsed[permname] = p.state;
                }
                else
                {
                    //We are a normal thingy
                    if (collapsed.TryGetValue(p.name, out var state))
                    {
                        if (p.state != State.Unset)
                            collapsed[p.name] = p.state;
                    }
                    else
                    {
                        collapsed.Add(p.name, p.state);
                    }
                }
            }

            //Get the ID's
            var roleIds = collapsed.Where(kp => kp.Value != State.Deny).Select(kp => kp.Key.Substring(11)).ToHashSet();

            //Apply the permissions
            await member.ReplaceRolesAsync(member.Guild.Roles.Values.Where(role => roleIds.Contains(role.Id.ToString())), reason: "Permission Sync");
        }
    }
}
