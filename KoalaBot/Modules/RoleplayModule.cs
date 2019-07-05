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
using KoalaBot.Permissions.CommandNext;
using DSharpPlus.Entities;

namespace KoalaBot.Modules
{
    [Group("roleplay"), Aliases("rp")]
    public class RoleplayModule : BaseCommandModule
    {
        public Koala Bot { get; }
        public IRedisClient Redis => Bot.Redis;
        public Logger Logger { get; }

        public RoleplayModule(Koala bot)
        {
            this.Bot = bot;
            this.Logger = new Logger("CMD-RP", bot.Logger);
        }

        /*
         * Hmm, maybe a command that lists off a player's characters via their account login to the Hub
         * That way we could find out if an influx of random characters causing trouble is simply just one person or 
         *  to help organise where to assign our warnings in the trello without accidently making a new one for the same player
         * 
         * 
         * 
         */

    }
}
