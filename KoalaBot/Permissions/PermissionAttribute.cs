using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
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
        public string Permission { get;  }
        public bool OwnerBypass { get; }
        public bool AdminBypass { get; }

        public PermissionAttribute(string perm, bool ownerBypass = true, bool adminBypass = true)
        {
            Permission = perm;
            OwnerBypass = ownerBypass;
            AdminBypass = adminBypass;
        }

        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            if (OwnerBypass && ctx.User.Id == ctx.Client.CurrentApplication.Owner.Id)
                return true;

            if (AdminBypass && ctx.Member.Roles.Any(r => r.Permissions.HasFlag(DSharpPlus.Permissions.Administrator)))
                return true;

            var manager = Koala.Bot.PermissionManager.GetGuildManager(ctx.Guild);
            if (manager == null) return false;

            var user = await manager.GetUserGroupAsync(ctx.Member);
            if (user == null) return false;

            return await user.EvaluateAsync(this.Permission) == State.Allow;
        }
    }
}
