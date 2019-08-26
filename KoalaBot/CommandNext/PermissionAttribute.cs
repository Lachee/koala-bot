using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using KoalaBot.Extensions;
using KoalaBot.PermissionEngine;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace KoalaBot.CommandNext
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PermissionAttribute : CheckBaseAttribute
    {

        public string PermissionName { get; }
        public bool AdminBypass { get; }
        public bool OwnerBypass { get; }
        public bool RestrictChannel { get; }

        /// <summary>
        /// Limits the command to a specific permission
        /// </summary>
        /// <param name="permission">The permission required</param>
        /// <param name="adminBypass">Should admins bypass it?</param>
        /// <param name="ownerBypass">Should owners bypass it?</param>
        /// <param name="restrictChannel">Should it restrict it to a specific channel?</param>
        public PermissionAttribute(string permission, bool adminBypass = false, bool ownerBypass = true, bool restrictChannel = true)
        {
            PermissionName = permission;
            OwnerBypass = ownerBypass;
            AdminBypass = adminBypass;
            RestrictChannel = restrictChannel;
            Permission.Record(permission);
        }

        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            //TODO: Fix This
            if (help)
            {
                return true;

                var group = await ctx.Member.GetGroupAsync();
                var pattern = PermissionName.Replace(".", "\\.") + "($|\\..*)";
                var permissions = await group.EvaluatePatternAsync(new System.Text.RegularExpressions.Regex(pattern));
                return permissions.Any(p => p.State == StateType.Allow);
            }


            //So we will always check the most specific one first, so we will append the channel id to the end of our permission.
            string perm = PermissionName + (RestrictChannel ? "." + ctx.Channel.Id : "");
            return await ctx.Member.HasPermissionAsync(perm, AdminBypass, OwnerBypass);
        }

    }
}
