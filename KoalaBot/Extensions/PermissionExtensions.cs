using DSharpPlus.Entities;
using KoalaBot.Entities;
using KoalaBot.Permissions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Extensions
{
    public static class PermissionExtensions
    {
        /// <summary>
        /// Checks if the member is allowed to use the permission
        /// </summary>
        /// <param name="member"></param>
        /// <param name="permission">The permission to check</param>
        /// <returns></returns>
        public static async Task<bool> HasPermissionAsync(this DiscordMember member, string permission, bool adminBypass = true, bool bypassOwner = true)
        {
            //Evaluate the permission and check if its true
            return (await CheckPermissionAsync(member, permission, adminBypass, bypassOwner)) == State.Allow;
        }

        /// <summary>
        /// Checks the state of the permission for the member.
        /// </summary>
        /// <param name="member"></param>
        /// <param name="permission">The permission to check</param>
        /// <returns></returns>
        public static async Task<State> CheckPermissionAsync(this DiscordMember member, string permission, bool adminBypass = true, bool bypassOwner = true)
        {
            //If we are the owner, just bypass
            if (bypassOwner || member.Id == Koala.Bot.Discord.CurrentApplication.Owner.Id)
                return State.Allow;
            
            if (adminBypass && member.Roles.Any(r => r.Permissions.HasFlag(DSharpPlus.Permissions.Administrator)))
                return State.Allow;

            //Get the group
            var group = await Koala.Bot.PermissionManager.GetGuildManager(member.Guild).GetUserGroupAsync(member);
            Debug.Assert(group != null);

            //Evaluate the permission
            return await group.EvaluateAsync(permission);
        }
    }
}
