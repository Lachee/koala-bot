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
using System.Diagnostics;
using System.Collections.Generic;
using KoalaBot.Managers;
using KoalaBot.CommandNext;
using KoalaBot.PermissionEngine.Groups;
using KoalaBot.PermissionEngine;

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
            var engine = group.Engine;
            await ctx.ReplyAsync(group.Name + " Permissions:\n```\n" + engine.ExportGroup(group) + "\n```");
        }


        [Command("export")]
        [Description("Exports all groups")]
        [Permission("koala.permissions.group.export")]
        public async Task ExportGroup(CommandContext ctx)
        {
            var engine = PermissionManager.GetEngine(ctx.Guild);
            var export = await engine.ExportAsync();

            if (export.Length < 1980)
            {
                await ctx.ReplyAsync("```\n" + export + "\n```");
            }
            else
            {
                string tmppath = "export_" + ctx.Guild.Id + "_" + ctx.Member.Username + ".txt";
                try
                {
                    await System.IO.File.WriteAllTextAsync(tmppath, export);
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
        [Description("Imports all")]
        [Permission("koala.permissions.group.import")]
        public async Task ImportAll(CommandContext ctx,
         [RemainingText][Description("The permissions to import, as a multiline codeblock.")] string import)
        {
            var sw = Stopwatch.StartNew();
            string content = import.Replace("`", "#");
            var engine = PermissionManager.GetEngine(ctx.Guild);
            await engine.ImportAsync(content);
            await ctx.ReplyAsync("Imported. Took " + sw.ElapsedMilliseconds + "ms");

        }

        [Command("tree")]
        [Description("Creates a visual representation of permission tree")]
        [Permission("koala.permissions.group.tree")]
        public async Task Tree(CommandContext ctx, Group group)
        {
            //Get teh user group
            if (group == null)
                throw new ArgumentNullException($"The group does not exist.");

            //Get the tree
            var tree = await TreeBranch.CreateTreeAsync(group);
            var sb = new StringBuilder();
            tree.BuildTreeString(sb);
            var export = sb.ToString();

            if (export.Length < 1980)
            {
                await ctx.ReplyAsync("```\n" + export + "\n```");
            }
            else
            {
                string tmppath = "tree_" + ctx.Guild.Id + "_" + group.Name + ".txt";
                try
                {
                    await System.IO.File.WriteAllTextAsync(tmppath, export);
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

        #region Create / Delete Groups
        [Command("create")]
        [Description("Creates a new empty group")]
        [Permission("koala.permissions.group.create")]
        public async Task CreateGroup(CommandContext ctx, string name)
        {
            //Validate the member
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("The group name cannot be null or empty.");

            if (name.StartsWith("group."))
                throw new ArgumentException("The group does not need to start with group.");

            //Create the group.
            var group = await PermissionManager.GetEngine(ctx.Guild).AddGroupAsync(name);
            await ctx.ReplyAsync($"Group `{group.Name}` was created.");
        }


        [Command("delete")]
        [Description("Deletes a group")]
        [Permission("koala.permissions.group.delete")]
        public async Task DeleteGroup(CommandContext ctx, Group group)
        {
            //Don't knokw what the group is
            if (group == null)
                throw new ArgumentNullException($"The group `{group.Name}` does not exist.");

            //Delete the group
            await group.DeleteAsync();
            await ctx.ReplyAsync($"Group `{group.Name}` was deleted.");
        }

        [Command("priority")]
        [Description("Sets a group's priority")]
        [Permission("koala.permissions.group.priority")]
        public async Task DeleteGroup(CommandContext ctx, Group group, int priority)
        {
            //Don't knokw what the group is
            if (group == null)
                throw new ArgumentNullException($"The group `{group.Name}` does not exist.");

            //Delete the group
            group.Priority = priority;
            var success = await group.SaveAsync();
            await ctx.ReplyReactionAsync(success);
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
            group.AddPermission(permission, StateType.Allow);
            var success = await group.SaveAsync();
            await ctx.ReplyReactionAsync(success);
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
            group.RemovePermission(permission);
            var success = await group.SaveAsync();
            await ctx.ReplyReactionAsync(success);
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
                group = await PermissionManager.GetMemberGroupAsync(ctx.Member);

            if (permission == null)
                throw new ArgumentNullException($"Permission cannot be empty.");

            //Evaluate the state
            Stopwatch watch = Stopwatch.StartNew();
            var state = await group.EvaluatePermissionAsync(permission);

            //Log out all the permissions
            await ctx.ReplyAsync($"**{ctx.Member.DisplayName}**\n`{permission}` => `{state}`\nTook _{watch.ElapsedMilliseconds}ms_");
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

        #endregion

        [Command("reload")]
        [Description("Destroys the complete cache")]
        [Permission("koala.permissions.reload", adminBypass: true, ownerBypass: true)]
        public async Task Reload(CommandContext ctx)
        {
            await PermissionManager.GetEngine(ctx.Guild).Store.ClearCacheAsync();
            await ctx.ReplyReactionAsync(true);
        }

        [Command("all")]
        [Description("lists all the permissions")]
        [Permission("koala.permissions.all")]
        public async Task All(CommandContext ctx)
        {
            //Join it all alphabetically and then respond with it
            string joined = string.Join("\n", Permission.RecordedPermissions.OrderBy(r => r));
            await ctx.ReplyAsync($"Permissions:\n```\n{joined}\n```");
        }
    }
}
