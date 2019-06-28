using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KoalaBot.Ticker
{
	class TickerMessage : ITickable
	{
        public ActivityType Activity { get; set; }
        public string Message { get; set; }

        public TickerMessage(string message) : this(ActivityType.Playing, message) { }
        public TickerMessage(ActivityType activity, string message)
        {
            Activity = activity;
            Message = message;
        }

        public Task<DiscordActivity> GetActivityAsync(TickerManager manager)
        {
            return Task.FromResult(new DiscordActivity(Message, Activity));
		}
	}
}
