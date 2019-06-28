using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Ticker
{
	class TickerGuildsAvailable : ITickable
	{
		public Task<DiscordActivity> GetActivityAsync(TickerManager manager)
        {
			int guilds = manager.Discord.Guilds.Count;
            return Task.FromResult(new DiscordActivity($"{guilds} guilds", ActivityType.ListeningTo));
		}
	}
}
