using DSharpPlus.Entities;
using KoalaBot.Entities;
using KoalaBot.Exceptions;
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
        /// Checks if the member is implicitly allowed to use the permission. Throws a PermissionException if they are not. 
        /// <para>Implicitly allowed means that it will return true if the state is <see cref="State.Allow"/> or <see cref="State.Unset"/></para>
        /// </summary>
        /// <param name="member"></param>
        /// <param name="permission">The permission to check</param>
        /// <returns></returns>
        public static async Task ThrowPermissionAsync(this DiscordMember member, string permission, bool bypassAdmin = true, bool bypassOwner = true, bool allowUnset = false)
        {
            //Evaluate the permission and check if its true
            if (! await HasPermissionAsync(member, permission, bypassAdmin, bypassOwner, allowUnset))
                throw new PermissionException(permission);
        }

        /// <summary>
        /// Checks if the member is allowed to use the permission
        /// </summary>
        /// <param name="member">The member to check</param>
        /// <param name="permission">The permission to check</param>
        /// <param name="bypassAdmin">Should admins always return true?</param>
        /// <param name="bypassOwner">Should owners always return true?</param>
        /// <param name="allowUnset">Should unset permissions be considered valid?</param>
        /// <returns></returns>
        public static async Task<bool> HasPermissionAsync(this DiscordMember member, string permission, bool bypassAdmin = true, bool bypassOwner = true, bool allowUnset = false)
        {
            //Evaluate the permission and check if its true
            var state = (await CheckPermissionAsync(member, permission, bypassAdmin, bypassOwner));
            return state == State.Allow || (allowUnset && state == State.Unset);
        }

        /// <summary>
        /// Checks the permission of the member and returns the <see cref="State"/> (Unset, Allow, Deny).
        /// </summary>
        /// <param name="member">The member to check</param>
        /// <param name="permission">The permission to check</param>
        /// <param name="bypassAdmin">Should admins always return true?</param>
        /// <param name="bypassOwner">Should owners always return true?</param>
        /// <returns></returns>
        public static async Task<State> CheckPermissionAsync(this DiscordMember member, string permission, bool bypassAdmin = true, bool bypassOwner = true)
        {
            //Try to add the permission to the attribute. We are doing this so the \perm all command can list dynamically added permissions too
            // Doing it this way results in \perm all being slightly inaccurate, but since it is just a indication anyways it is fine
            Permission.Record(permission);

            //Check the bypasses
            if (bypassOwner && member.Id == Koala.Bot.Discord.CurrentApplication.Owner.Id)
                return State.Allow;
            
            if (bypassAdmin && member.Roles.Any(r => r.Permissions.HasFlag(DSharpPlus.Permissions.Administrator)))
                return State.Allow;

            //Get the group
            var group = await Koala.Bot.PermissionManager.GetGuildManager(member.Guild).GetMemberGroupAsync(member);
            Debug.Assert(group != null);

            //Evaluate the permission
            return await group.EvaluatePermissionAsync(permission);
        }
    }
}
