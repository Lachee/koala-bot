using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using KoalaBot.Redis;
using KoalaBot.Extensions;
using KoalaBot.Logging;
using System.IO;
using KoalaBot.Permissions.CommandNext;
using DSharpPlus.Entities;
using System.Text.RegularExpressions;
using System.Data;

namespace KoalaBot.Modules
{
    [Group("fun"), Aliases("f", "util")]
    [Description("A bunch of fun commands")]
    public class FunModule : BaseCommandModule
    {
        private readonly Regex DiceRegex = new Regex(@"(?'count'\d{1,2})[dD](?'sides'\d{1,3})", RegexOptions.Compiled);

        public Koala Bot { get; }
        public IRedisClient Redis => Bot.Redis;
        public Logger Logger { get; }
        private Random _diceRandom;

        public FunModule(Koala bot)
        {
            this.Bot = bot;
            this.Logger = new Logger("CMD-FUN", bot.Logger);
        }

        [Command("roll"), Aliases("r")]
        [Description("Rolls dice")]
        [Permission("koala.fun.roll")]
        public async Task Roll(CommandContext ctx, [RemainingText] string expression)
        {
            //Set the seed and create a new dice random.
            int seed = (int)(ctx.Message.Id % int.MaxValue);
            _diceRandom = new Random(seed);

            //Prepare the expresion and result
            string expr = Regex.Replace(expression.ToLowerInvariant(), "\\d*[dD]\\d+", DiceRegexReplacer);
            double result = 0;

            //Evaluate
            try
            {
                DataTable table = new DataTable();
                table.Columns.Add("expression", typeof(string), expr);
                var row = table.NewRow();
                table.Rows.Add(row);
                result = double.Parse((string)row["expression"]);

                //Return the result
                await ctx.ReplyAsync(content: $"🎲 `{result}`\n        --------\n        `{expression}`\n        `{expr}`");
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to roll dice.");
                await ctx.ReplyReactionAsync(DiscordEmoji.FromUnicode("❓"));
            }
        }


        public async Task SetBoosterEmoji(CommandContext ctx)
        {
            if (ctx.Message.Attachments.Count == 0)
                throw new ArgumentException("Must contain attachement.");

            await SetBoosterEmoji(ctx, ctx.Message.Attachments[0].Url);
        }

        public async Task SetBoosterEmoji(CommandContext ctx, string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException("url");


        }

        private string DiceRegexReplacer(Match match)
        {
            int tally = 0;
            uint count, sides;
            string[] parts = match.Value.Split('d');


            //Make sure its parsed correctly
            if (parts.Length != 2) return match.Value;
            if (!uint.TryParse(parts[0], out count)) count = 1;
            if (!uint.TryParse(parts[1], out sides)) sides = 6;

            //Roll the dice numerous times
            Random random = _diceRandom ?? new Random();
            for (uint i = 0; i < count; i++)
                tally += random.Next(1, (int)sides);

            //return the tally
            return tally.ToString();
        }

    }
}
