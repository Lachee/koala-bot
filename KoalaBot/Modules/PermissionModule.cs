using System;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using KoalaBot.Redis;
using KoalaBot.Extensions;
using KoalaBot.Logging;
using DSharpPlus.Entities;
using KoalaBot.Entities;
using System.Linq;
using KoalaBot.Permissions;
using System.Diagnostics;
using KoalaBot.Permissions.CommandNext;
using System.Collections.Generic;
using KoalaBot.Managers;

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
        [RequireOwner]
        public async Task Test(CommandContext ctx, Group group, string permission)
        {
            Stopwatch watch = Stopwatch.StartNew();
            StringBuilder results = new StringBuilder("Finished\n");

            for (int i = 0; i < 3; i++)
            {
                watch.Restart();
                await group.EvaluatePermissionAsync(permission);
                results.Append(watch.ElapsedTicks).Append(" ticks").Append('\n');
            }

            await ctx.ReplyAsync(results.ToString());
        }

        #region Listing
        [Command("list")]
        [Description("Lists all the permissions a member has")]
        [Permission("koala.permissions.member.list")]
        public async Task List(CommandContext ctx, [Description("The group to check")] MemberGroup memberGroup = null)
        {
            //Validate the member
            if (memberGroup == null)
                memberGroup = await Bot.PermissionManager.GetGuildManager(ctx.Guild).GetMemberGroupAsync(ctx.Member);

            //Log out all the permissions
            await ctx.ReplyAsync(memberGroup.Username + " Permissions:\n```\n" + string.Join("\n", memberGroup.ExportEnumerable(true)) + "\n```");
        }

        //[Command("list")]
        //[Description("Lists all the sub permissions a member has")]
        //[Permission("koala.permissions.member.list")]
        //public async Task List(CommandContext ctx, [Description("The group to check")] MemberGroup memberGroup, string parent)
        //{
        //    //Validate the member
        //    if (memberGroup == null)
        //        memberGroup = await Bot.PermissionManager.GetGuildManager(ctx.Guild).GetMemberGroupAsync(ctx.Member);
        //
        //    //Get the subs
        //    var subs = await memberGroup.EvaluatePermissionChildrenAsync(parent);
        //
        //    //Log out all the permissions
        //    await ctx.RespondContentAsync(memberGroup.Username + " Permissions:\n```\n" + string.Join("\n", subs.Select(p => p.name)) + "\n```");
        //}

        [Command("list")]
        [Description("Lists all the permissions a group has")]
        [Permission("koala.permissions.group.list")]
        public async Task List(CommandContext ctx, [Description("The group to check")] Group group)
        {
            //Validate the member
            if (group == null)
                throw new ArgumentNullException("The group cannot be null.");

            //Log out all the permissions
            await ctx.ReplyAsync(group.Name + " Permissions:\n```\n" + string.Join("\n", group.ExportEnumerable()) + "\n```");
        }

        [Command("list")]
        [Description("Lists all the sub permissions a permission has")]
        [Permission("koala.permissions.group.list")]
        public async Task Sub(CommandContext ctx, [Description("The group to check")] Group group, string parent)
        {
            string name = group.Name;

            //Validate the member
            if (group == null)
                throw new ArgumentNullException("The group cannot be null.");

            if (string.IsNullOrEmpty(parent))
                throw new ArgumentNullException("The parent permission cannot be null");

            if (group is MemberGroup mg)
            {
                await ctx.Member.ThrowPermissionAsync("koala.permissions.member.list");
                name = mg.DisplayName;
            }

            //Get the subs
            var subs = await group.EvaluatePermissionChildrenAsync(parent);

            //Log out all the permissions
            await ctx.ReplyAsync(name + " Permissions:\n```\n" + string.Join("\n", subs.Select(p => p.name)) + "\n```");
        }


        [Command("export")]
        [Description("Exports a group")]
        [Permission("koala.permissions.group.export")]
        public async Task ExportGroup(CommandContext ctx, [Description("The group to export. Cannot be a user or role group.")] Group group)
        {
            //Get teh user group
            if (group == null)
                throw new ArgumentNullException($"The group does not exist.");

            //We want a group group, not a member group or a role group
            if (group.Name.StartsWith("group.role") || group.Name.StartsWith("group.user"))
                throw new ArgumentException("Cannot export role or user groups.");
            
            //Report the permissions
            //var enumerable = group is MemberGroup ? ((MemberGroup)group).ToEnumerable(true) : group.ToEnumerable();
            var enumerable = group.ExportEnumerable();
            await ctx.ReplyAsync(group.Name + " Permissions:\n```\n" + string.Join("\n", enumerable) + "\n```");
        }


        [Command("export")]
        [Description("Exports all groups")]
        public async Task ExportGroup(CommandContext ctx)
        {
            var guildManager = PermissionManager.GetGuildManager(ctx.Guild);
            var groupNames = await guildManager.FindGroupsAsync();

            List<string> lines = new List<string>();
            long characters = 0;
            
            foreach (var groupName in groupNames.OrderBy(l => l))
            {
                var g = await guildManager.GetGroupAsync(groupName);
                lines.Add("");
                lines.Add(groupName);
                characters += 2 + groupName.Length;

                foreach (var l in g.ExportEnumerable())
                {
                    lines.Add("\t" + l);
                    characters += 2 + l.Length;
                }

            }

            if (characters < 1900)
            {
                await ctx.ReplyAsync("```\n" + string.Join('\n', lines) + "\n```");
            }
            else
            {
                string tmppath = "export_" + ctx.Guild.Id + "_" + ctx.Member.Username + ".txt";
                try
                {
                    await System.IO.File.WriteAllLinesAsync(tmppath, lines);
                    await ctx.RespondWithFileAsync(tmppath, "Exported Groups:");
                    await ctx.ReplyReactionAsync(true);
                }
                catch (Exception)
                {
                    await ctx.ReplyReactionAsync(false);
                }
                finally
                {
                    if (System.IO.File.Exists(tmppath))
                        System.IO.File.Delete(tmppath);
                }
            }
        }

        [Command("import")]
        [Description("Imports a group")]
        [Permission("koala.permissions.group.import")]
        public async Task ImportGroup(CommandContext ctx, 
            [Description("The name of the group to override. Cannot be a user or role group.")] string groupname, 
            [RemainingText][Description("The permissions to import, as a multiline codeblock.")] string import)
        {
            string inner = import.Trim('`', '\n');
            string[] lines = inner.Split('\n');

            //Make sure the group name starts with correct wording.
            if (!groupname.StartsWith("group."))
                groupname = "group." + groupname;

            //Make sure its not a group or user
            if (groupname.StartsWith("group.role") || groupname.StartsWith("group.user"))
                throw new ArgumentException("Cannot import role or user groups.");
            
            //Get group, otherwise create
            Group group = await Bot.PermissionManager.GetGuildManager(ctx.Guild).GetGroupAsync(groupname);
            if (group == null)
                group = await Bot.PermissionManager.GetGuildManager(ctx.Guild).CreateGroupAsync(groupname);

            //Assign the group permissions
            group.ImportEnumerable(lines);

            //save it
            await group.SaveAsync();
            await ctx.ReplyAsync($"Group Imported: `{group.Name}`");
        }

        [Command("tree")]
        [Description("Calculates a visual representation of permission tree")]
        public async Task Tree(CommandContext ctx, Group group)
        {
            //Get teh user group
            if (group == null)
                throw new ArgumentNullException($"The group does not exist.");

            //Get the tree
            var tree = await group.EvaluatePermissionTree();
            await ctx.ReplyAsync($"```\n{tree.CollapseDown()}\n```");
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
            await ctx.ReplyAsync($":greentick: Group `{group.Name}` was created.");
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
            await ctx.ReplyAsync($":greentick: Group `{group.Name}` was deleted.");
        }
        #endregion

        #region Add Permissions
       
        [Command("add")]
        [Description("Adds a permission to a group")]
        [Permission("koala.permissions.group.add", adminBypass: true, ownerBypass: true)]
        public async Task AddPermission(CommandContext ctx, Group group, string permission)
        {
            //Validate the member
            if (group == null)
                throw new ArgumentNullException("The group cannot be null or empty.");

            //Validate the permission
            if (string.IsNullOrEmpty(permission))
                throw new ArgumentNullException("The permission cannot be null or empty.");
            
            //Add the permission then save
            await group.AddPermissionAsync(permission, true);
            await ctx.ReplyReactionAsync(true);
        }
        #endregion

        #region Remove Permissions
        [Command("remove")]
        [Permission("koala.permissions.group.remove")]
        public async Task RemovePermission(CommandContext ctx, Group group, string permission)
        {
            //Validate the member
            if (group == null)
                throw new ArgumentNullException("The group cannot be null or empty.");

            //Validate the permission
            if (string.IsNullOrEmpty(permission))
                throw new ArgumentNullException("The permission cannot be null or empty.");

            //Remove the permission then save
            await group.RemovePermissionAsync(permission, true);
            await ctx.ReplyReactionAsync(true);
        }

        #endregion

        #region Check

        [Command("check")]
        [Permission("koala.permissions.member.check", adminBypass: true, ownerBypass: true)]
        [Description("Checks if the user has a specified permission")]
        public async Task CheckPermission(CommandContext ctx, MemberGroup group, string permission)
        {
            //Checks th e users group
            if (group == null)
                group = await PermissionManager.GetGuildManager(ctx.Guild).GetMemberGroupAsync(ctx.Member);

            if (permission == null)
                throw new ArgumentNullException($"Permission cannot be empty.");

            //Evaluate the state
            Stopwatch watch = Stopwatch.StartNew();
            var state = await group.EvaluatePermissionAsync(permission);

            //Log out all the permissions
            await ctx.ReplyAsync($"**{group.Username}**\n`{permission}` => `{state}`\nTook _{watch.ElapsedMilliseconds}ms_");
        }

        [Command("check")]
        [Permission("koala.permissions.group.check", adminBypass: true, ownerBypass: true)]
        [Description("Checks if the user has a specified permission")]
        public async Task CheckPermission(CommandContext ctx, Group group, string permission)
        {
            //Checks th e users group
            if (group == null)
                throw new ArgumentNullException($"The group does not exist.");

            if (permission == null)
                throw new ArgumentNullException($"Permission cannot be empty.");

            //Evaluate the state
            Stopwatch watch = Stopwatch.StartNew();
            var state = await group.EvaluatePermissionAsync(permission);

            //Log out all the permissions
            await ctx.ReplyAsync($"**{group.Name}**\n`{permission}` => `{state}`\nTook _{watch.ElapsedMilliseconds}ms_");
        }

        [Command("groups")]
        public async Task Groups(CommandContext ctx)
        {
            var guildManager = PermissionManager.GetGuildManager(ctx.Guild);
            var groups = await guildManager.FindGroupsAsync();
            await ctx.ReplyAsync("**Groups:**\n```\n" + string.Join("\n", groups) + "\n```");
        }

        #endregion

        [Command("apply"), Aliases("rolesync", "syncrole")]
        [Description("Applies the roles to the user.")]
        [Permission("koala.permissions.apply")]
        public async Task ApplyRoles(CommandContext ctx, 
            [Description("The user to sync. Leave as null to apply your own roles.")] DiscordMember member = null)
        {
            if (member == null)
                member = ctx.Member;

            await PermissionManager.ApplyRolesAsync(member);
            await ctx.ReplyReactionAsync(true);
        }

        [Command("reload")]
        [Description("Destroys the complete cache")]
        [Permission("koala.permissions.reload", adminBypass: true, ownerBypass: true)]
        public async Task Reload(CommandContext ctx)
        {
            PermissionManager.GetGuildManager(ctx.Guild).Reload();
            await ctx.ReplyReactionAsync(true);
        }

        [Command("all")]
        [Description("lists all the permissions")]
        [Permission("koala.permissions.all")]
        public async Task All(CommandContext ctx)
        {
            //Join it all alphabetically and then respond with it
            string joined = string.Join("\n", Permission.Recorded.OrderBy(r => r));
            await ctx.ReplyAsync($"Permissions:\n```\n{joined}\n```");
        }
    }
}
