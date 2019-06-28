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
        public ulong Id => Member.Id;
        public string Username => Member.Username;
        public string DisplayName => Member.DisplayName;


        public MemberGroup(GuildManager manager, string name, DiscordMember member, int cacheCapacity = 10) : base(manager, name, cacheCapacity)
        {
            Member = member;
        }

        
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

        protected override IEnumerable<Permission> GetPermissionEnumerable()
        {
            yield return new Permission(GuildManager.DEFAULT_GROUP, State.Allow);
            yield return new Permission(Manager.GetRoleGroupName(Member.Guild.EveryoneRole));
            foreach (var role in Member.Roles.OrderBy(r => r.Position))
                yield return new Permission(Manager.GetRoleGroupName(role));

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
            foreach (var role in Member.Roles.OrderBy(r => r.Position))
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

#if AOINDSIOA



        /// <summary>
        /// Evaluates the permission for the group
        /// </summary>
        /// <param name="permission"></param>
        /// <returns></returns>
        public override async Task<State> EvaluateAsync(string permission)
        {
            //Prepare the state of the permission
            State state = State.Unset;

            //Continiously loop through the permissions, slowly getting smaller and smaller
            while (state == State.Unset && !string.IsNullOrEmpty(permission))
            {
                //Check all our role permissions
                foreach (var role in Member.Roles.OrderBy(r => r.Position))
                {
                    //Get the role group so we can evaluate it
                    var group = await this.Manager.GetRoleGroupAsync(role);
                    if (group != null)
                    {
                        //We found the group, so we will now evaluate what it had and if its not unset we will use that.
                        var gstate = await group.EvaluateAsync(permission);
                        if (gstate != State.Unset) state = gstate;
                    }
                }

                //Check all our permissions
                foreach (Permission perm in defines)
                {
                    if (perm.isGroup)
                    {
                        //We are a group, so we need to get the group from memory
                        var group = await this.Manager.GetGroupAsync(perm.name);
                        if (group != null)
                        {
                            //We found the group, so we will now evaluate what it had and if its not unset we will use that.
                            var gstate = await group.EvaluateAsync(permission);
                            if (gstate != State.Unset) state = gstate;
                        }
                    }
                    else if (perm.name.Equals(permission))
                    {
                        //This permission matches, if its not unset then we will apply it
                        if (perm.state != State.Unset)
                            state = perm.state;
                    }
                }


                //We havn't found a state yet, so we will shrink it now
                if (state == State.Unset)
                {
                    //Get the permission of the dot. If there isnt any, break early.
                    int dot = permission.LastIndexOf('.');
                    if (dot <= 0) break;

                    //Trim upto the dot
                    permission = permission.Substring(0, dot - 1);
                }
            }

            return state;
        }


#endif