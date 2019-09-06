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
        [Group("restore")]
        [Description("Handles world restores.")]
        public partial class RestoreModule : BaseCommandModule
        {
            public Koala Bot { get; }
            public IRedisClient Redis => Bot.Redis;
            public Logger Logger { get; }
            public StarwatchClient Starwatch => Bot.Starwatch;

            public RestoreModule(Koala bot)
            {
                this.Bot = bot;
                this.Logger = new Logger("CMD-SW-PROC", bot.Logger);
            }

            [GroupCommand]
            [Permission("sw.backup.list")]
            [Description("Lists a world's backup setting.")]
            public async Task GetRestore(CommandContext ctx, World world)
            {
                if (world == null)
                    throw new ArgumentNullException("world");

                if (!await ctx.Member.HasPermissionAsync($"sw.backup.list.{world.Whereami}", false, allowUnset: true))
                    throw new PermissionException($"sw.backup.list.{world.Whereami}");

                //Fetch the response
                await ctx.ReplyWorkingAsync();
                var response = await Starwatch.GetRestoreAsync(world);

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
                await ctx.ReplyAsync($"**{world.Whereami}** is set to auto restore. " + (response.Payload.Mirror != null ? "It mirrors `" + response.Payload.Mirror + "`" : ""));
            }

            [Command("create")]
            [Permission("sw.backup.create")]
            [Description("Creates a snapshot of the world to restore. ")]
            public async Task CreateRestoreSnapshot(CommandContext ctx, World world)
            {
                if (world == null)
                    throw new ArgumentNullException("world");

                //Fetch the response
                await ctx.ReplyWorkingAsync();
                var response = await Starwatch.CreateRestoreAsync(world);

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
                await ctx.ReplyAsync($"**{world.Whereami}** is set to auto restore. " + (response.Payload.Mirror != null ? "It mirrors `" + response.Payload.Mirror + "`" : ""));
            }


            [Command("mirror")]
            [Permission("sw.backup.mirror")]
            [Description("Sets the worlds mirror. ")]
            public Task RemoveRestoreMirror(CommandContext ctx, World world) => MirrorRestore(ctx, world, null);

            [Command("mirror")]
            public async Task MirrorRestore(CommandContext ctx, World world, World mirror)
            {
                if (world == null)
                    throw new ArgumentNullException("world");

                //Fetch the response
                await ctx.ReplyWorkingAsync();
                var response = await Starwatch.SetRestoreMirrorAsync(world, mirror);

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
                await ctx.ReplyAsync($"**{world.Whereami}** is set to auto restore. " + (response.Payload.Mirror != null ? "It mirrors `" + response.Payload.Mirror + "`" : ""));
            }

            [Command("delete"), Aliases("d")]
            [Permission("sw.backup.delete")]
            [Description("Deletes a world's restore setting")]
            public async Task DeleteRestore(CommandContext ctx,
                [Description("The world to delete the backup for.")] World world)
            {
                if (world == null)
                    throw new ArgumentNullException("world");

                if (!await ctx.Member.HasPermissionAsync($"sw.backup.delete.{world.Whereami}", false, allowUnset: true))
                    throw new PermissionException($"sw.backup.delete.{world.Whereami}");

                //Fetch the response
                await ctx.ReplyWorkingAsync();
                var response = await Starwatch.DeleteRestoreAsync(world);

                //404, the world doesn't exist
                if (response.Status == RestStatus.ResourceNotFound)
                {
                    await ctx.ReplyAsync(world + " does not exist and cannot be deleted.");
                    return;
                }

                //Something else, throw an exception
                if (response.Status != RestStatus.OK)
                    throw new RestResponseException(response);

                await ctx.ReplyAsync("Restore configuration for `" + world + "` has been deleted.");
            }

        }
    }
}
