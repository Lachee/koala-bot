using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Ticker
{
	class TickerRandom : ITickable
	{
		Random random = new Random();
		string[] messages = new string[] 
		{
			"with Redis",
			"with D#+",
			"with .NET Core 2.1",
			"awooo",
			".NET Core is the future",
			"sick beatsies",
			"like nobodies busniess",
			"Parappa the Rapper",
			"sweat salty tears",
			"DarkSouls III",
			"with B1nzy's Pings",
			"with C++ meme libs", 
			"with R. Danny",
			"trash",
			"with Raid",
			"PRAISE GREAT EMZI, THE GODEST OF EMZU AND EMU KIND",
			"dead",
			"with his food",
            "with Wompat",
			"only Mei and Junkrat",
			"Cory in the House Theme Song 24 Hour Mix.",
			"Wii Song 1 Hour verison",
			"We are Number One",
			"We are number one but everytime robbie rotten speaks it gets a little bit faster",
			"the entire bee movie script",
			"Party Crashers",

		};

		public Task<DiscordActivity> GetActivityAsync(TickerManager manager)
        {
			int r = random.Next(messages.Length);
            return Task.FromResult(new DiscordActivity(messages[r], ActivityType.Playing));
		}
	}
}
