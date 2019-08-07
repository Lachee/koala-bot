using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KoalaBot.Permissions
{
    public class MemberGroup : Group
    {
        public DiscordMember Member { get; }
        public DiscordGuild Guild => Member.Guild;
        public ulong Id => Member.Id;
        public string Username => Member.Username;
        public string DisplayName => Member.DisplayName;


        public MemberGroup(GuildManager manager, string name, DiscordMember member, int cacheCapacity = 10) : base(manager, name, cacheCapacity)
        {
            Member = member;
        }


        /// <summary>
        /// Gets the members roles.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<DiscordRole> GetRolesEnumerable() => Member.Roles;

        /// <summary>
        /// Serializes into an enumerable list of permissions.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> ExportEnumerable(bool includeRoles)
        {
            if (includeRoles)
            {
                foreach (var role in Member.Roles)
                    yield return Manager.GetRoleGroupName(role);
            }

            foreach (var s in base.ExportEnumerable())
                yield return s;
        }

        /// <summary>
        /// Gets the permissions enumerable
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<Permission> GetPermissionEnumerable()
        {
            //Add the default group & the everybody role
            yield return new Permission(GuildManager.DEFAULT_GROUP, State.Allow);
            yield return new Permission(Manager.GetRoleGroupName(Member.Guild.EveryoneRole));

            //Add all the roles, in order of their position
            foreach (var role in GetRolesEnumerable().OrderBy(r => r.Position))
                yield return new Permission(Manager.GetRoleGroupName(role));

            //Add all our defines last.
            foreach (var p in defines)
                yield return p;
        }

        public override async Task<PermTree> EvaluatePermissionTree(PermTree tree = null)
        {
            //Create the tree if it doesnt exist
            if (tree == null)
                tree = new PermTree(this);

            //Make sure the group is valid.
            if (tree.Group != this)
                throw new ArgumentException("PermTree does not have the correct group.");

            //Add all our permissions and groups
            foreach (var role in GetRolesEnumerable().OrderBy(r => r.Position))
            {
                //Get the role group so we can evaluate it
                var group = await this.Manager.GetRoleGroupAsync(role);
                if (group != null)
                {
                    //Create a child and evaluate it
                    var child = tree.AddChild(group);
                    await group.EvaluatePermissionTree(child);
                }
                else
                {
                    tree.AddPermission(new Permission(group.Name));
                }
            }

            //Now do the base stuff
            return await base.EvaluatePermissionTree(tree);
        }
    }
}