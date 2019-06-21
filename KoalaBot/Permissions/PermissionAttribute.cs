using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using KoalaBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Permissions
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PermissionAttribute : CheckBaseAttribute
    {
        public string Permission { get; }
        public bool AdminBypass { get; }
        public bool OwnerBypass { get; }

        public PermissionAttribute(string perm, bool adminBypass = true, bool ownerBypass = true)
        {
            Permission = perm;
            OwnerBypass = ownerBypass;
            AdminBypass = adminBypass;
        }

        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            return await ctx.Member.HasPermissionAsync(Permission, AdminBypass, OwnerBypass);
        }
    }
}
