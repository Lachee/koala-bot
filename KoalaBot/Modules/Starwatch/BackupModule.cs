using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using KoalaBot.CommandNext;
using KoalaBot.Exceptions;
using KoalaBot.Extensions;
using KoalaBot.Logging;
using KoalaBot.Redis;
using KoalaBot.Starwatch;
using KoalaBot.Starwatch.Entities;
using KoalaBot.Starwatch.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Modules.Starwatch
{
    public partial class StarwatchModule
    {
        [Group("backup")]
        [Description("Handles world backups.")]
        public partial class BackupModule : BaseCommandModule
        {
            public Koala Bot { get; }
            public IRedisClient Redis => Bot.Redis;
            public Logger Logger { get; }
            public StarwatchClient Starwatch => Bot.Starwatch;

            public BackupModule(Koala bot)
            {
                this.Bot = bot;
                this.Logger = new Logger("CMD-SW-PROC", bot.Logger);
            }

            [GroupCommand]
            [Permission("sw.backup.list")]
            [Description("Lists a world's backup setting.")]
            public async Task GetBackup(CommandContext ctx, World world)
            {
                if (world == null)
                    throw new ArgumentNullException("world");

                if (!await ctx.Member.HasPermissionAsync($"sw.backup.list.{world.Whereami}", false, allowUnset: true))
                    throw new PermissionException($"sw.backup.list.{world.Whereami}");

                //Fetch the response
                await ctx.ReplyWorkingAsync();
                var response = await Starwatch.GetBackupAsync(world);

                //404, the world doesn't exist
                if (response.Status == RestStatus.ResourceNotFound)
                {
                    await ctx.ReplyAsync(world + " is not backed up.");
                    return;
                }

                //Something else, throw an exception
                if (response.Status != RestStatus.OK)
                    throw new RestResponseException(response);

                //Build the response
                DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
                builder.WithTitle($"Backup {world.Whereami}")
                    .AddField("Is Rolling", response.Payload.IsRolling.ToString())
                    .AddField("Is Auto Restore", response.Payload.IsAutoRestore.ToString())
                    .AddField("Last Time", response.Payload.LastBackup.ToString());

                await ctx.ReplyAsync(embed: builder.Build());
            }

            [Command("edit"), Aliases("e")]
            [Permission("sw.backup.edit")]
            [Description("Edits a world's backup setting")]
            public async Task EditBackup(CommandContext ctx, World world,
                [Description("Enables regular automatic backups of the world. ")] bool isRolling,
                [Description("Makes the world automatically restore from its latest backup on restart.")] bool isAutoRestore)
            {
                if (world == null)
                    throw new ArgumentNullException("world");

                if (!await ctx.Member.HasPermissionAsync($"sw.backup.edit.{world.Whereami}", false, allowUnset: true))
                    throw new PermissionException($"sw.backup.edit.{world.Whereami}");

                //Fetch the response
                await ctx.ReplyWorkingAsync();
                var backup = new Backup()
                {
                    IsAutoRestore = isAutoRestore,
                    IsRolling = isRolling,
                    LastBackup = null
                };

                var response = await Starwatch.EditBackupAsync(world, backup);

                //404, the world doesn't exist
                if (response.Status == RestStatus.ResourceNotFound)
                {
                    await ctx.ReplyAsync(world + " does not exist and cannot be backed up.");
                    return;
                }

                //Something else, throw an exception
                if (response.Status != RestStatus.OK)
                    throw new RestResponseException(response);

                //Build the response
                DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
                builder.WithTitle($"Backup {world.Whereami}")
                    .AddField("Is Rolling", response.Payload.IsRolling.ToString())
                    .AddField("Is Auto Restore", response.Payload.IsAutoRestore.ToString())
                    .AddField("Last Time", response.Payload.LastBackup.ToString());

                await ctx.ReplyAsync(embed: builder.Build());
            }

            [Command("delete"), Aliases("d")]
            [Permission("sw.backup.delete")]
            [Description("Deletes a world's backup setting")]
            public async Task EditBackup(CommandContext ctx,
                [Description("The world to delete the backup for.")] World world, 
                [Description("Delete the previous backed up files.")] bool deleteFiles)
            {
                if (world == null)
                    throw new ArgumentNullException("world");

                if (!await ctx.Member.HasPermissionAsync($"sw.backup.delete.{world.Whereami}", false, allowUnset: true))
                    throw new PermissionException($"sw.backup.delete.{world.Whereami}");

                //Fetch the response
                await ctx.ReplyWorkingAsync();
                var response = await Starwatch.DeleteBackupAsync(world, deleteFiles);

                //404, the world doesn't exist
                if (response.Status == RestStatus.ResourceNotFound)
                {
                    await ctx.ReplyAsync(world + " does not exist and cannot be deleted.");
                    return;
                }

                //Something else, throw an exception
                if (response.Status != RestStatus.OK)
                    throw new RestResponseException(response);

                await ctx.ReplyAsync("Backup configuration for `" + world + "` has been deleted.");
            }

        }
    }
}
