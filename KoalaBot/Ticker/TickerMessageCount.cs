using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Ticker
{
	class TickerMessageCount : ITickable
	{
		public async Task<DiscordActivity> GetActivityAsync(TickerManager manager)
        {
            var messages = await manager.Bot.MessageCounter.GetGlobalCountAsync();
            return new DiscordActivity($"{messages} messages", ActivityType.Watching);
		}
	}
}
