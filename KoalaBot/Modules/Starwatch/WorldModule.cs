using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using KoalaBot.Extensions;
using KoalaBot.Logging;
using KoalaBot.Permissions.CommandNext;
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
        [Group("world"), Aliases("w")]
        [Description("Handles various world commands.")]
        public partial class WorldModule : BaseCommandModule
        {
            public Koala Bot { get; }
            public IRedisClient Redis => Bot.Redis;
            public Logger Logger { get; }
            public StarwatchClient Starwatch => Bot.Starwatch;

            public WorldModule(Koala bot)
            {
                this.Bot = bot;
                this.Logger = new Logger("CMD-SW-PROC", bot.Logger);
            }

            [GroupCommand]
            [Permission("sw.world.list")]
            public async Task GetWorldDetails(CommandContext ctx, World world)
            {
                await ctx.ReplyAsync(world.Whereami);
                throw new NotImplementedException();
            }

            [Command("alias")]
            [Description("Sets the specified alias.")]
            public async Task SetAlias(CommandContext ctx, string alias, World world = null)
            {
                //World is null, so delete the alias
                if (world == null)
                {
                    var success = await Redis.RemoveAsync(Namespace.Combine(ctx.Guild, "starwatch", "world-alias", alias));
                    if (success)
                        await ctx.ReplyAsync("World Removed");
                    else
                        await ctx.ReplyReactionAsync(false);
                }
                else
                {
                    //Sets the alias. Note that this is used in the WorldConverter.
                    await Redis.StoreStringAsync(Namespace.Combine(ctx.Guild, "starwatch", "world-alias", alias), world.Whereami);
                    await ctx.ReplyReactionAsync(true);
                }
            }
        }
    }
}
