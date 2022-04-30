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
using DSharpPlus.Entities;
using System.Text.RegularExpressions;
using System.Data;
using System.Linq;
using KoalaBot.CommandNext;

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
        private int _explosionTally;

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
            _explosionTally = 0;

            //Prepare the expresion and result
            //string expr = Regex.Replace(expression.ToLowerInvariant(), "\\d*[dD]\\d+", DiceRegexReplacer);
            string expr = Regex.Replace(expression, "\\d*[dD]\\d+", EvaluateDiceRegex);
            double result = 0;

            //Evaluate
            try
            {
                DataTable table = new DataTable();
                table.Columns.Add("expression", typeof(string), expr);
                var row = table.NewRow();
                table.Rows.Add(row);
                result = double.Parse((string)row["expression"]);

                string explosions = "";
                if (_explosionTally > 0) explosions += $"\n\nExploded `{_explosionTally}` times";

                //Return the result
                await ctx.ReplyAsync(content: $"🎲 `{result}`\n        --------\n        `{expression}`\n        `{expr}`" + explosions);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to roll dice.");
                await ctx.ReplyReactionAsync(DiscordEmoji.FromUnicode("❓"));
            }
        }

        [Permission("koala.fun.emoji")]
        public async Task SetBoosterEmoji(CommandContext ctx)
        {
            if (ctx.Message.Attachments.Count == 0)
                throw new ArgumentException("Must contain attachement.");

            await SetBoosterEmoji(ctx, ctx.Message.Attachments[0].Url);
        }

        [Permission("koala.fun.emoji")]
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
        private string EvaluateDiceRegex(Match match)
        {
            List<uint> results = EvaluateDice(match, out var explosions);
            _explosionTally += explosions;

            return "(" + string.Join('+', results) + ")";
        }

        /// <summary>
        /// Evaluates a dice, returning the rolls it made and the number of explosions.
        /// </summary>
        /// <param name="match"></param>
        /// <param name="explosions"></param>
        /// <returns></returns>
        private List<uint> EvaluateDice(Match match, out int explosions)
        {

            uint count, sides;
            bool exploding;
            explosions = 0;

            //Parse the dice
            if (!TryParseDice(match, out sides, out count, out exploding))
                return null;

            //Prepare the dice and the tally
            List<uint> rolls    = new List<uint>(1);
            Random random       = _diceRandom ?? new Random();

            explosions = RollDice(rolls, sides, count, exploding);
            return rolls;
        }

        /// <summary>
        /// Roll a specified sided dice, a specified number of times, exploding it if allowed.
        /// </summary>
        /// <param name="rolls">The rolls that have been made</param>
        /// <param name="sides">The number of sides the dice has.</param>
        /// <param name="count">The number of dice to roll</param>
        /// <param name="explodes">Should the dice explode?</param>
        /// <returns></returns>
        private int RollDice(List<uint> rolls, uint sides, uint count, bool explodes)
        {
            int explosions  = 0;
            Random random   = _diceRandom ?? new Random();

            for (uint i = 0; i < count; i++)
            {
                //Roll the dice and add the results.
                uint result = (uint) random.Next(1, (int) sides + 1);
                rolls.Add(result);

                //If the dice explodes, roll the dice again.
                if (explodes && result == sides)
                    explosions += 1 + RollDice(rolls, sides, 1, explodes);
            }

            //Return the number of times we exploded
            return explosions;
        }

        private bool TryParseDice(Match match, out uint sides, out uint count, out bool exploding)
        {
            string[] parts = match.Value.Split('d', 'D');

            exploding = match.Value.Contains('D');
            sides = 6;
            count = 1;

            //Make sure its parsed correctly
            if (parts.Length != 2) return false;
            if (!uint.TryParse(parts[0], out count) || count == 0) count = 1;
            if (!uint.TryParse(parts[1], out sides) || sides <= 1) sides = 6;

            return true;
        }
    }
}
