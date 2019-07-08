using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KoalaBot.CommandNext
{
    public class CommandQueryArgumentConverter : IArgumentConverter<CommandQuery>
    {
        public Task<Optional<CommandQuery>> ConvertAsync(string value, CommandContext ctx)
        {
            //Get the matches
            var matches = QueryConverter.REGEX_MATCH.Matches(value);
            if (matches.Count == 0) return Task.FromResult(Optional.FromNoValue<CommandQuery>());

            //Prepare the query
            CommandQuery query = new CommandQuery(ctx);
            foreach (Match m in matches)
            {
                string kpair = m.Groups[2].Value;
                string vpair = m.Groups[3].Value;
                query.Add(kpair, vpair);
            }

            return Task.FromResult(Optional.FromValue(query));
        }
    }

    public class QueryConverter : IArgumentConverter<Query>
    {
        public static Regex REGEX_MATCH = new Regex(@"((\w+)\s?[:=]\s?(\S*)\s*)", RegexOptions.Compiled);
        public Task<Optional<Query>> ConvertAsync(string value, CommandContext ctx)
        {
            var matches = REGEX_MATCH.Matches(value);
            if (matches.Count == 0) return Task.FromResult(Optional.FromNoValue<Query>());

            //Prepare the query
            Query query = new Query();
            foreach(Match m in matches)
            {
                string kpair = m.Groups[2].Value;
                string vpair = m.Groups[3].Value;
                query.Add(kpair, vpair);
            }

            return Task.FromResult(Optional.FromValue(query));
        }
    }
}
