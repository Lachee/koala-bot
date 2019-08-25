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
		static readonly Random random = new Random();
		static readonly string[] messages = new string[] 
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
			"with Emzi",
            "with Sharon",
            "with Xandium",
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
            "Stallion's erotic Visual Novel",
            "cruoton",
            "albion online",
            "starbound",
            "bacon like a damn fiddle",
            "Kerbal Space Program 2",
            "Rainbow Six: Siege",
            "in VR",
            "Party Golf",
            "Party Crashers",
            "Risk of Rain",
            "Risk of Rain 2",
            "Leathal League",
            "Leathal League: Blaze",
            "Minecraft",
            "SUPER HOT",
            "Garry's Mod",
            "Factorio",
            "Dark Souls II",
            "Cities Skylines",
            "Rocket League",
            "Just Cause 3",
            "Just Cause 2",
            "Sid Meier''s Civilization V",
            "Terraria",
            "Gimbal",
            "DRAGON BALL XENOVERSE 2",
            "Deatho's heart strings",
            "on Voyager Roleplay",
            "Overwatch"
		};

		public Task<DiscordActivity> GetActivityAsync(TickerManager manager)
        {
			int r = random.Next(messages.Length);
            return Task.FromResult(new DiscordActivity(messages[r], ActivityType.Playing));
		}
	}
}
