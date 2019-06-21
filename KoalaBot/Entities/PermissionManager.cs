using DSharpPlus.Entities;
using KoalaBot.Logging;
using KoalaBot.Permissions;
using KoalaBot.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Entities
{
    public class PermissionManager
    {
        /*
            koala.ban                           Ban from server
            koala.kick                          Kick from server
            koala.silence                       Global role removal and mute

            koala.mute                          Mute from any channel
            koala.mute.389940452855382017       Mute from specific channel

            koala.see                           Allows role to see that channel

        (Everyone Role)
        group.364299133445734410:
            koala.see.389940452855382017

        (Admin Role)
        group.364297010112757781:
            group.364299133445734410
            ~koala.see.389940452855382017

        */

        public Koala Bot { get; }
        public Logger Logger { get; }
        public IRedisClient Redis => Bot.Redis;
        private Dictionary<ulong, GuildManager> _guilds;

        public PermissionManager(Koala bot, Logger logger = null)
        {
            Bot = bot;
            Logger = logger ?? new Logger("PERMS");
            _guilds = new Dictionary<ulong, GuildManager>();

            Bot.Discord.GuildAvailable += (args) =>
            {
                Logger.Log("Loading Guild {0}", args.Guild);
                _guilds.Add(args.Guild.Id, new GuildManager(Bot, args.Guild, Logger.CreateChild(args.Guild.Name)));
                return Task.CompletedTask;
            };

            Bot.Discord.GuildUnavailable += (args) =>
            {
                Logger.Log("Clearing Guild {0}", args.Guild);
                _guilds.Remove(args.Guild.Id);
                return Task.CompletedTask;
            };

            Bot.Discord.GuildMemberRemoved += async (args) =>
            {
                Logger.Log("Deleting Member {0}", args.Member);
                var manager = GetGuildManager(args.Guild);
                var group = await manager.GetUserGroupAsync(args.Member);
                await group.DeleteAsync();
            };

            Bot.Discord.GuildRoleDeleted += async (args) =>
            {
                Logger.Log("Deleting Role {0}", args.Role);
                var manager = GetGuildManager(args.Guild);
                var group = await manager.GetRoleGroupAsync(args.Role);
                await group.DeleteAsync();
            };
        }

        public GuildManager GetGuildManager(DiscordGuild guild) => _guilds[guild.Id];
    }
}
