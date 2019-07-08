using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
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
        public Task<Optional<World>> ConvertAsync(string value, CommandContext ctx)
        {
            var world = World.Parse(value);
            if (world != null) return Task.FromResult(Optional.FromValue(world));
            return Task.FromResult(Optional.FromNoValue<World>());
        }
    }
}
