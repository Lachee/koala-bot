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
        private Logging.Logger Logger { get; set; }

        public TickerStarwatch(StarwatchClient client)
        {
            this.Client = client;
            Logger = new Logging.Logger("TICK");
        }

		public async Task<DiscordActivity> GetActivityAsync(TickerManager manager)
        {
            try
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

            catch (Exception e)
            {
                // Typically either SSL or target machine refused.
                Logger.LogError(e.Message);
#if DEBUG
                return new DiscordActivity("[Debug] Starwatch offline.", ActivityType.Watching);
#else
                return new DiscordActivity("Starwatch offline.", ActivityType.Watching);
#endif
            }
        }
	}
}
