using DSharpPlus.Entities;
using KoalaBot.Extensions;
using KoalaBot.Logging;
using KoalaBot.PermissionEngine;
using KoalaBot.PermissionEngine.Groups;
using KoalaBot.PermissionEngine.Store;
using KoalaBot.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Managers
{
    public class PermissionManager : Manager
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

        private const int MAX_ROLE_DEPTH = 50;

        private Dictionary<ulong, Engine> _guildEngines;

        public PermissionManager(Koala bot, Logger logger = null) : base(bot, logger)
        {
            _guildEngines = new Dictionary<ulong, Engine>();

            Bot.Discord.GuildAvailable += (args) =>
            {
                Logger.Log("Loading Guild {0}", args.Guild);
                var gm = new Engine(new RedisStore(Redis, Namespace.Join(Namespace.RootNamespace, args.Guild.Id.ToString(), "perms")));
                _guildEngines[args.Guild.Id] = gm;
                return Task.CompletedTask;
            };

            Bot.Discord.GuildUnavailable += (args) =>
            {
                Logger.Log("Clearing Guild {0}", args.Guild);
                _guildEngines.Remove(args.Guild.Id);
                return Task.CompletedTask;
            };

            /*
            Bot.Discord.GuildMemberRemoved += async (args) =>
            {
                Logger.Log("Deleting Member {0}", args.Member);
                var manager = GetEngine(args.Guild);
                var group = await manager.GetMemberGroupAsync(args.Member);
                await group.DeleteAsync();
            };
            */

            Bot.Discord.GuildRoleDeleted += async (args) =>
            {
                Logger.Log("Deleting Role {0}", args.Role);
                var engine = GetEngine(args.Guild);
                var group = await engine.GetGroupAsync(args.Role.GetGroupName());
                if (group != null) await engine.DeleteGroupAsync(group);
            };
            
        }
                
        public Engine GetEngine(DiscordGuild guild) => _guildEngines[guild.Id];
        
        public async Task<MemberGroup> GetMemberGroupAsync(DiscordMember member)
        {
            var engine = GetEngine(member.Guild);

            var group = await engine.GetGroupAsync(MemberGroup.GetGroupName(member));
            if (group is MemberGroup memberGroup) return memberGroup;

            var mg = new MemberGroup(engine, member);
            await engine.AddGroupAsync(mg);
            return mg;
        }

        /// <summary>
        /// Applies the missing roles too the member. It will also remove roles that have been specifically denied.
        /// Returns true if a change has occured.
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public async Task<bool> ApplyRolesAsync(DiscordMember member)
        {
            var stopwatch = Stopwatch.StartNew();
            var mg = await GetMemberGroupAsync(member);

            //Get all the group permissions
            var roleGroups = await mg.EvaluatePatternAsync(new System.Text.RegularExpressions.Regex(@"group\.role\..*"));
            var rolesAfter = member.Roles.ToDictionary<DiscordRole, ulong>(r => r.Id);
            bool changed = false;

            foreach(var group in roleGroups)
            {
                ulong id = ulong.Parse(group.GroupName.Substring(5));
                switch (group.State)
                {
                    case StateType.Deny:
                        if (rolesAfter.Remove(id))
                            changed = true;
                        break;

                    case StateType.Allow:
                        if (!rolesAfter.ContainsKey(id) && member.Guild.Roles.TryGetValue(id, out var tmprole)) 
                        {
                            rolesAfter[id] = tmprole;
                            changed = true;
                        }
                        break;

                    default:
                    case StateType.Unset:
                        break;
                }

            }


            //Apply the roles
            if (changed)
                await member.ReplaceRolesAsync(rolesAfter.Values, "Permission Sync");

            //Tell the world
            Logger.Log("Synced Group Roles for {0}. Took {1}ms", member, stopwatch.ElapsedMilliseconds);

            //Return the state
            return true;
        }

        /*
        private async Task OnGroupSaved(LegacyPermissions.Events.GroupEventArgs e)
        {
            if (e.Group is MemberGroup mg)
                await this.ApplyRolesAsync(mg.Member);
        }

        /// <summary>
        /// Applies the missing roles too the member. It will also remove roles that have been specifically denied.
        /// Returns true if a change has occured.
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public async Task<bool> ApplyRolesAsync(DiscordMember member)
        {
            var stopwatch = Stopwatch.StartNew();
            var mg = await GetMemberGroupAsync(member);
            var dmg = new DynamicMemberGroup(mg);

            //Calculate the roles
            bool change = await ApplyRolesAsync(dmg);

            //Apply the roles
            if (change)            
                await member.ReplaceRolesAsync(dmg.GetRolesEnumerable(), "Permission Sync");

            //Tell the world
            Logger.Log("Synced Group Roles for {0}. Took {1}ms", member, stopwatch.ElapsedMilliseconds);

            //Return the state
            return true;
        }

        /// <summary>
        /// Recursively applies the roles until no difference is determined.
        /// </summary>
        /// <param name="dmg"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        private async Task<bool> ApplyRolesAsync(DynamicMemberGroup dmg, int depth = 0)
        {
            //Get all the groups, prepending the existing roles.
            // We will be only getting roles that exist in the current guild.
            var roles = await CalculateMemberRolesAsync(dmg, guildOnly: true);
            if (dmg.ReplaceRoles(roles))
            {
                //Make sure we havn't hit max depth
                if (depth > MAX_ROLE_DEPTH)
                    throw new Exception("Recursive Loop determined to be never ending.");

                //There was a change, lets try it again
                await ApplyRolesAsync(dmg, depth + 1);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates all the roles a member should have.
        /// </summary>
        /// <param name="member"></param>
        /// <param name="guildOnly">Culls roles that are not in the guild.</param>
        /// <returns></returns>
        private async Task<HashSet<ulong>> CalculateMemberRolesAsync(MemberGroup mg, bool guildOnly = true)
        {
            //Get the role mapping
            var rolemap = await CollapseMemberRoleStateAsync(mg, true, guildOnly);

            //Get all the roles we are allowed
            return rolemap.Where(kp => kp.Value != State.Deny)
                            .Select(kp => ulong.Parse(kp.Key.Substring(11)))
                            .ToHashSet();
        }

        /// <summary>
        /// Collapses the roles of a member
        /// </summary>
        /// <param name="member">The member to calculate the roles for.</param>
        /// <param name="prependExistingRoles">Appends the members current roles as unset defaults to begin with.</param>
        /// <param name="guildOnly">Culls roles that are not in the guild.</param>
        /// <returns></returns>
        private async Task<Dictionary<string, State>> CollapseMemberRoleStateAsync(MemberGroup mg, bool prependExistingRoles, bool guildOnly = true)
        {
            //PRepare a stopwatch
            var stopwatch = Stopwatch.StartNew();

            //Get all the role groups
            var permissions = await mg.EvaluatePermissionChildrenAsync("group.role");

            //Collapse all the groups into a map of their state
            Dictionary<string, State> rolemap = new Dictionary<string, State>(permissions.Count);

            //If we have been told to, we will append the members current roles
            if (prependExistingRoles)
                foreach(var role in mg.GetRolesEnumerable())
                    rolemap.Add($"group.role.{role.Id}", State.Unset);

            //ITerate over every permission, appending it to the dictionary
            foreach (var permission in permissions)
            {
                if (permission.name.Length <= 11)
                {
                    //This is a universal modifier for all previous roles (eg: -group.role)
                    // We will apply the state to all the roles we already have. This way, BlackBacon can have -group.roles to remove all roles.
                    foreach (var permname in rolemap.Keys.ToList())
                        rolemap[permname] = permission.state;
                }
                else
                {
                    //If we are guild only, make sure this role exist in the guild
                    if (guildOnly)
                    {
                        //Make sure the ID is valid, and if it is, make sure it is actually a guild role.
                        // If we fail these checks, then we will just skip this item and not add it to our list.
                        if (!ulong.TryParse(permission.name.Substring(11), out var id) || !mg.Guild.Roles.ContainsKey(id))
                            continue;
                    }

                    //We are a normal role, so make sure we don't already have the role.
                    if (rolemap.TryGetValue(permission.name, out var state))
                    {
                        //We already have the role, 
                        // we don't want to set the role to unset when it already has a value.
                        if (permission.state != State.Unset)
                            rolemap[permission.name] = permission.state;
                    }
                    else
                    {
                        //We do not have the role, so we will add it.
                        rolemap.Add(permission.name, permission.state);
                    }
                }
            }

            Logger.Log("Collapsed Roles for {0}, Took {1} ms", mg.Member, stopwatch.ElapsedMilliseconds);
            return rolemap;
        }
    */
    }
}
