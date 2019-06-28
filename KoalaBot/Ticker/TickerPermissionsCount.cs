using DSharpPlus.Entities;
using KoalaBot.Permissions;
using KoalaBot.Permissions.CommandNext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Ticker
{
	class TickerPermissionsCount : ITickable
	{
		public Task<DiscordActivity> GetActivityAsync(TickerManager manager)
        {
            int count = Permission.Recorded.Count();
            return Task.FromResult(new DiscordActivity($"{count} permissions", ActivityType.Watching));
		}
	}
}
