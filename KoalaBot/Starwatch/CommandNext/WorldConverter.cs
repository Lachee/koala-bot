using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using KoalaBot.Redis;
using KoalaBot.Starwatch.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KoalaBot.Starwatch.CommandNext
{
    public class WorldConverter : IArgumentConverter<World>
    {  
        public Koala Bot { get; }
        public IRedisClient Redis => Bot.Redis;

        public WorldConverter(Koala koala)
        {
            Bot = koala;
        }

        public async Task<Optional<World>> ConvertAsync(string value, CommandContext ctx)
        {
            //Check for alias
            var alias = await Redis.FetchStringAsync(Namespace.Combine(ctx.Guild, "starwatch", "world-alias", value));
            alias = alias ?? value;

            //Parse the world
            var world = World.Parse(alias);
            if (world != null) return Optional.FromValue(world);
            return Optional.FromNoValue<World>();
        }
    }
}
