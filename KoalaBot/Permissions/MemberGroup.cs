using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KoalaBot.Permissions
{
    public class MemberGroup : Group
    {
        public DiscordMember Member { get; set; }

        public MemberGroup(GuildManager manager, string name, DiscordMember member) : base(manager, name)
        {
            Member = member;
        }

        
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
                    if (perm.group)
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
    }
}
