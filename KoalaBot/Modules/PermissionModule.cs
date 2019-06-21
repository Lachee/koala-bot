using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using KoalaBot.Redis;
using KoalaBot.Extensions;
using KoalaBot.Logging;
using DSharpPlus.Entities;
using KoalaBot.Entities;
using KoalaBot.Exceptions;
using System.Linq;
using KoalaBot.Permissions;
using System.Diagnostics;

namespace KoalaBot.Modules
{
    [Group("perm"), RequirePermissions(DSharpPlus.Permissions.ManageMessages)]
    public class PermissionModule : BaseCommandModule
    {
        public Koala Bot { get; }
        public PermissionManager PermissionManager => Bot.PermissionManager;
        public IRedisClient Redis => Bot.Redis;
        public Logger Logger { get; }

        public PermissionModule(Koala bot)
        {
            this.Bot = bot;
            this.Logger = new Logger("CMD-MOD", bot.Logger);
        }

        [Command("test")]
        [Permission("koala.permissions.test", false)]
        public async Task Test(CommandContext ctx)
        {
            await ctx.RespondReactionAsync(true);
        }

        #region Listing
        [Command("list")]
        [Description("Lists all the permissions the user has")]
        [Permission("koala.permissions.member.list")]
        public async Task List(CommandContext ctx, [Description("Optional user to check.")] DiscordMember member = null)
        {
            //Validate the member
            if (member == null) member = ctx.Member;

            //Select the group
            var group = await PermissionManager.GetGuildManager(member.Guild).GetUserGroupAsync(member);

            //Log out all the permissions
            await ctx.RespondAsync(member.Username + " Permissions:\n```\n" + string.Join("\n", group.ToEnumerable()) + "\n```");
        }

        [Command("list")]
        [Description("Lists all the permissions the role has")]
        [Permission("koala.permissions.role.list")]
        public async Task List(CommandContext ctx, [Description("The role to check")] DiscordRole role)
        {
            //Validate the member
            if (role == null)
                throw new ArgumentNullException("The role cannot be null.");

            //Get teh user group
            var group = await PermissionManager.GetGuildManager(ctx.Guild).GetRoleGroupAsync(role);
            if (group == null)
                throw new ArgumentNullException($"The group `{role}` does not exist.");

            //Logout the permissions
            await ctx.RespondAsync(role + " Permissions:\n```\n" + string.Join("\n", group.ToEnumerable()) + "\n```");
        }

        [Command("list")]
        [Description("Lists all the permissions the group has")]
        [Permission("koala.permissions.group.list")]
        public async Task List(CommandContext ctx, [Description("Name of the group to check")] string name)
        {
            //Validate the member
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("The group name cannot be null or empty.");

            if (!name.StartsWith("group."))
                name = "group." + name;

            //Get teh user group
            var group = await PermissionManager.GetGuildManager(ctx.Guild).GetGroupAsync(name);
            if (group == null)
                throw new ArgumentNullException($"The group `{name}` does not exist.");

            //Logout the permissions
            await ctx.RespondAsync(name + " Permissions:\n```\n" + string.Join("\n", group.ToEnumerable()) + "\n```");
        }

        #endregion

        #region Create / Delete Groups
        [Command("create")]
        [Description("Creates a new empty group")]
        [Permission("koala.permissions.group.create")]
        public async Task CreateGroup(CommandContext ctx, string name)
        {
            //Validate the member
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("The group name cannot be null or empty.");

            //Make sure it starts with group.
            if (!name.StartsWith("group."))
                name = "group." + name;

            //Get teh user group
            var group = await PermissionManager.GetGuildManager(ctx.Guild).CreateGroupAsync(name);
            await ctx.RespondAsync($":greentick: Group `{group.Name}` was created.");
        }


        [Command("delete")]
        [Description("Deletes a group")]
        [Permission("koala.permissions.group.delete")]
        public async Task DeleteGroup(CommandContext ctx, string name)
        {
            if (!name.StartsWith("group."))
                name = "group." + name;

            //Get teh user group
            var group = await PermissionManager.GetGuildManager(ctx.Guild).GetGroupAsync(name);
            if (group == null)
                throw new ArgumentNullException($"The group `{name}` does not exist.");

            //Delete the group
            await group.DeleteAsync();
            await ctx.RespondAsync($":greentick: Group `{group.Name}` was deleted.");
        }
        #endregion

        #region Add Permissions
        [Command("add")]
        [Permission("koala.permissions.member.add")]
        public async Task AddPermission(CommandContext ctx, DiscordMember member, string permission)
        {
            //Validate the member
            if (member == null)
                throw new ArgumentNullException("The member cannot be null.");

            //Validate the permission
            if (string.IsNullOrEmpty(permission))
                throw new ArgumentNullException("The permission cannot be null or empty.");

            //Get teh user group
            var group = await PermissionManager.GetGuildManager(member.Guild).GetUserGroupAsync(member);
            Debug.Assert(group != null);

            //Set the permission and save the permission manager
            await group.AddPermissionAsync(permission);
            await ctx.RespondReactionAsync(true);
        }

        [Command("add")]
        [Permission("koala.permissions.role.add")]
        public async Task AddPermission(CommandContext ctx, DiscordRole role, string permission)
        {
            //Validate the member
            if (role == null)
                throw new ArgumentNullException("The member cannot be null.");

            //Validate the permission
            if (string.IsNullOrEmpty(permission))
                throw new ArgumentNullException("The permission cannot be null or empty.");

            //Get teh user group
            var group = await PermissionManager.GetGuildManager(ctx.Guild).GetRoleGroupAsync(role);
            Debug.Assert(group != null);

            //Set the permission and save the permission manager
            await group.AddPermissionAsync(permission);
            await ctx.RespondReactionAsync(true);
        }

        [Command("add")]
        [Permission("koala.permissions.group.add")]
        public async Task AddPermission(CommandContext ctx, string name, string permission)
        {
            //Validate the member
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("The group name cannot be null or empty.");

            //Validate the permission
            if (string.IsNullOrEmpty(permission))
                throw new ArgumentNullException("The permission cannot be null or empty.");
            
            if (!name.StartsWith("group."))
                name = "group." + name;

            //Get teh user group
            var group = await PermissionManager.GetGuildManager(ctx.Guild).GetGroupAsync(name);
            if (group == null)
                throw new ArgumentNullException($"The group `{name}` does not exist.");

            //Add the permission then save
            await group.AddPermissionAsync(permission);
            await ctx.RespondReactionAsync(true);
        }
        #endregion

        #region Remove Permissions
        [Command("remove")]
        [Permission("koala.permissions.member.remove")]
        public async Task RemovePermission(CommandContext ctx, DiscordMember member, string permission)
        {

        }

        [Command("remove")]
        [Permission("koala.permissions.role.remove")]
        public async Task RemovePermission(CommandContext ctx, DiscordRole role, string permission)
        {

        }

        [Command("remove")]
        [Permission("koala.permissions.group.remove")]
        public async Task RemovePermission(CommandContext ctx, string group, string permission)
        {

        }
        #endregion

        #region Check

        [Command("check")]
        [Permission("koala.permissions.member.check")]
        [Description("Checks if the user has a specified permission")]
        public async Task CheckPermission(CommandContext ctx, DiscordMember member, string permission)
        {
            //Validate the member
            if (member == null) member = ctx.Member;

            if (permission == null)
                throw new ArgumentNullException($"Permission cannot be empty.");

            //Select the group
            var group = await PermissionManager.GetGuildManager(member.Guild).GetUserGroupAsync(member);

            //Evaluate the state
            Stopwatch watch = Stopwatch.StartNew();
            var state = await group.EvaluateAsync(permission);

            //Log out all the permissions
            await ctx.RespondAsync($"**{member.Username}**\n`{permission}` => `{state}`\nTook _{watch.ElapsedMilliseconds}ms_");
        }

        [Command("check")]
        [Permission("koala.permissions.role.check")]
        [Description("Checks if the role has a specified permission")]
        public async Task CheckPermission(CommandContext ctx, DiscordRole role, string permission)
        {
            //Validate the member
            if (role == null)
                throw new ArgumentException("Role cannot be null.");

            if (permission == null)
                throw new ArgumentNullException($"Permission cannot be empty.");

            //Select the group
            var group = await PermissionManager.GetGuildManager(ctx.Guild).GetRoleGroupAsync(role);

            //Evaluate the state
            Stopwatch watch = Stopwatch.StartNew();
            var state = await group.EvaluateAsync(permission);

            //Log out all the permissions
            await ctx.RespondAsync($"**{role.Name}**\n`{permission}` => `{state}`\nTook _{watch.ElapsedMilliseconds}ms_");
        }

        [Command("check")]
        [Permission("koala.permissions.role.check")]
        [Description("Checks if the role has a specified permission")]
        public async Task CheckPermission(CommandContext ctx, string groupName, string permission)
        {
            //Validate the member
            if (string.IsNullOrEmpty(groupName))
                throw new ArgumentException("group cannot be null.");

            if (permission == null)
                throw new ArgumentNullException($"Permission cannot be empty.");

            //Select the group
            var group = await PermissionManager.GetGuildManager(ctx.Guild).GetGroupAsync(groupName);

            //Evaluate the state
            Stopwatch watch = Stopwatch.StartNew();
            var state = await group.EvaluateAsync(permission);

            //Log out all the permissions
            await ctx.RespondAsync($"**{groupName}**\n`{permission}` => `{state}`\nTook _{watch.ElapsedMilliseconds}ms_");
        }

        #endregion

        [Command("reload")]
        [Description("Destroys the complete cache")]
        [Permission("koala.permissions.reload")]
        public async Task Reload(CommandContext ctx)
        {
            PermissionManager.GetGuildManager(ctx.Guild).Reload();
            await ctx.RespondReactionAsync(true);
        }

        [Command("all")]
        [Description("lists all the permissions")]
        [Permission("koala.permissions.all")]
        public async Task All(CommandContext ctx)
        {
            //Prepare the list and iterate over all the root commands
            List<string> lines = new List<string>();
            foreach(var cmd in Bot.CommandsNext.RegisteredCommands)
            {
                //Add all our permission attributes
                lines.AddRange(cmd.Value.ExecutionChecks
                               .Select(a => (a as PermissionAttribute)?.Permission)
                               .Where(a => !string.IsNullOrEmpty(a))
                               .ToList());

                //Look for our children
                if (cmd.Value is CommandGroup group)
                {
                    //Iterate over all the children
                    foreach(var child in group.Children)
                    {
                        //Add the children
                        lines.AddRange(child.ExecutionChecks
                                        .Select(a => a as PermissionAttribute)
                                        .Where(a => a != null)
                                        .Select(a => a.Permission)
                                        .ToList());
                    }
                }
            }

            //Join it all alphabetically and then respond with it
            string joined = string.Join("\n", lines.OrderBy(r => r));
            await ctx.RespondAsync($"Permissions:\n```\n{joined}\n```");
        }
    }
}
