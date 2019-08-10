using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KoalaBot.Permissions
{
    /// <summary>
    /// A member group with manual definition of the roles
    /// </summary>
    public class DynamicMemberGroup : MemberGroup
    {
        public Dictionary<ulong, DiscordRole> Roles { get; set; }

        public DynamicMemberGroup(MemberGroup group, int cacheCapacity = 10) : base (group.Manager, group.Name, group.Member, cacheCapacity)
        {
            CopyGroup(group);
            SetRoles(group.Member.Roles);
        }

        /// <summary>
        /// Gets the members roles.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<DiscordRole> GetRolesEnumerable()
        {
            foreach(var r in Roles)
            {
                if (r.Key != Guild.EveryoneRole.Id)
                    yield return r.Value;
            }
        }

        /// <summary>
        /// Sets all the roles
        /// </summary>
        /// <param name="enumerable"></param>
        public void SetRoles(IEnumerable<DiscordRole> enumerable)
        {
            var dic = enumerable.ToDictionary<DiscordRole, ulong>((r) => r.Id);
            Roles = dic;
        }

        public bool AddRole(DiscordRole role)
        {
            if (role.Id == Guild.EveryoneRole.Id)
                return false;

            return Roles.TryAdd(role.Id, role);
        }

        /// <summary>
        /// Adds all the roles in the enumerable to the group
        /// </summary>
        /// <param name="enumerable"></param>
        public bool AddRoles(IEnumerable<DiscordRole> enumerable)
        {
            bool change = false;
            foreach (var r in enumerable)
                if (AddRole(r))
                    change = true;
            return change;
        } 
        
        /// <summary>
        /// Adds all the roles in the enumerable to the group
        /// </summary>
        /// <param name="enumerable"></param>
        public bool AddRoles(IEnumerable<ulong> enumerable)
        {
            bool change = false;
            foreach (var id in enumerable)
                if (id != Guild.EveryoneRole.Id && Guild.Roles.TryGetValue(id, out var role))
                    if (AddRole(role))
                        change = true;
            return change;
        }

        public bool ReplaceRoles(IEnumerable<ulong> enumerable)
        {
            var enumerated = enumerable.Where(u => u != Member.Guild.EveryoneRole.Id).ToList();
            var intersection = Roles.Keys.Intersect(enumerated).ToList();
            bool difference = intersection.Count != Roles.Keys.Count || intersection.Count != enumerated.Count;
            if (difference)
            {
                Roles.Clear();
                AddRoles(enumerable);
                return true;
            }

            return false;
        }
    }
}