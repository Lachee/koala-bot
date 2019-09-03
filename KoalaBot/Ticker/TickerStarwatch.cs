using DSharpPlus.Entities;
using KoalaBot.Starwatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Ticker
{
	class TickerStarwatch : ITickable
	{
        public StarwatchClient Client { get; }

        public TickerStarwatch(StarwatchClient client)
        {
            this.Client = client;
        }

		public async Task<DiscordActivity> GetActivityAsync(TickerManager manager)
        {
            var stats = await Client.GetStatisticsAsync();
            if (!stats.Success) throw new Exception("Statistics Failed.");

            string text;
            int connections = stats.Payload.Connections;

            switch (connections)
            {
                default:
                    text = connections + " Players";
                    break;

                case 0:
                    text = "Nobody";
                    break;

                case 1:
                    text = "A single player";
                    break;
            }


            return new DiscordActivity(text, ActivityType.Watching);
		}
	}
}
