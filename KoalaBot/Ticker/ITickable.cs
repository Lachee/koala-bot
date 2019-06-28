using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Ticker
{
	public interface ITickable
	{
		Task<DiscordActivity> GetActivityAsync(TickerManager manager);
	}
}
