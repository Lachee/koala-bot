using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using KoalaBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Permissions.CommandNext
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PermissionAttribute : CheckBaseAttribute
    {
  

        public string Permission { get; }
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
            Permission = permission;
            OwnerBypass = ownerBypass;
            AdminBypass = adminBypass;
            RestrictChannel = restrictChannel;
            Permissions.Permission.Record(permission);
        }

        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            //So we will always check the most specific one first, so we will append the channel id to the end of our permission.
            string perm = Permission + (RestrictChannel ? "." + ctx.Channel.Id : "");
            return await ctx.Member.HasPermissionAsync(perm, AdminBypass, OwnerBypass);
        }
    }
}
