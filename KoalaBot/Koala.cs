using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Net.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using KoalaBot.Entities;
using KoalaBot.Extensions;
using KoalaBot.Logging;
using KoalaBot.Modules;
using KoalaBot.Redis;
using KoalaBot.Redis.Serialize;

namespace KoalaBot
{
    public class Koala : IDisposable
    {
        /// <summary>
        /// Static instance of the current bot.
        /// </summary>
        public static Koala Bot { get; private set; }

        public BotConfig Configuration { get; }
        public DiscordClient Discord { get; }
        public CommandsNextExtension CommandsNext { get; }
        public Logger Logger { get; }
        public IRedisClient Redis { get; }
        public MessageCounter MessageCounter { get; }
        public ModWatcher ModWatcher { get; }
        public PermissionManager PermissionManager { get; }

        private Dictionary<ulong, string> _guildPrefixes = new Dictionary<ulong, string>(); 

        #region Initialization
        public Koala(BotConfig config)
        {
            Bot = this;
            this.Logger = new Logger("BOT", null);
            this.Configuration = config;

            //Configure redis
            Logger.Log("Creating new Stack Exchange Client");
            this.Redis = new StackExchangeClient(config.Redis.Address, config.Redis.Database, Logger.CreateChild("REDIS"));
            Namespace.SetRoot(config.Redis.Prefix);


            //Configure Discord
            Logger.Log("Creating new Bot Configuration");
            this.Discord = new DiscordClient(new DiscordConfiguration() { Token = config.Token });

            //Make sure the user isn't updating to bypass moderative actions
            Logger.Log("Creating Instances....");
            ModWatcher = new ModWatcher(this);
            PermissionManager = new PermissionManager(this, Logger.CreateChild("PERM"));

            //Track how many messages are sent
            Logger.Log("Creating Message Counter");
            this.MessageCounter = new MessageCounter(this, config.MessageCounterSyncRate * 1000);
            this.MessageCounter.ChangesSynced += async (args) =>
            {
                string tallyString = await Redis.FetchStringAsync(Namespace.Combine("global", "posts"), "0");
                await Discord.UpdateStatusAsync(new DiscordActivity($"{tallyString} msgs", ActivityType.Watching));
            };

            //Setup some deps
            Logger.Log("Creating Dependencies & Registering Commands");
            var deps = new ServiceCollection()
                .AddSingleton(this)
                .BuildServiceProvider(true);
            this.CommandsNext = this.Discord.UseCommandsNext(new CommandsNextConfiguration() { PrefixResolver = ResolvePrefix, Services = deps });
            this.CommandsNext.RegisterCommands(Assembly.GetExecutingAssembly());


            //Catch when any errors occur in the command handler
            //Send any command errors back after logging it.
            Logger.Log("Registering Error Listeners");
            this.Discord.ClientErrored += async (error) => await LogException(error.Exception);
            this.CommandsNext.CommandErrored += async (error) =>
            {
                Logger.LogError(error.Exception);
                await error.Context.RespondExceptionAsync(error.Exception, false);
            };

            Logger.Log("Done");
        }

        /// <summary>Initializes the bot</summary>
        /// <returns></returns>
        public async Task InitAsync()
        {
            Logger.Log("Initializing Redis");
            await Redis.InitAsync();

            Logger.Log("Connecting to Discord");
            await Discord.ConnectAsync();

            Logger.Log("Done!");
        }

        #endregion

        /// <summary>
        /// Resolves the prefix of the message and returns a index to trim from.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task<int> ResolvePrefix(DiscordMessage message)
        {
            try
            {
                if (message.Author == null || message.Author.IsBot) return -1;

                //Get the prefix. If we fail to find the prefix then we will get it from the cache
                string prefix;
                if (!_guildPrefixes.TryGetValue(message.Channel.GuildId, out prefix))
                {
                    Logger.Log("Prefix Cache Miss. Fetching new prefix for guild " + message.Channel.GuildId);
                    prefix = await Redis.FetchStringAsync(Namespace.Combine(message.Channel.GuildId, "prefix"), Configuration.Prefix);
                    await UpdatePrefix(message.Channel.Guild, prefix);
                }

                //Make sure we are allowed to execute in this channel
                var gm = await message.Channel.Guild.GetMemberAsync(message.Author.Id);
                if (!await gm.HasPermissionAsync($"koala.cmd.{message.ChannelId}")) return -1;

                //Return the length
                return message.GetStringPrefixLength(prefix);
            }
            catch(Exception e)
            {
                this.Logger.LogError(e);
                return -1;
            }
        }

        /// <summary>Updates the prefix of the guild</summary>
        public async Task UpdatePrefix(DiscordGuild guild, string prefix)
        {
            Logger.Log("Updating prefix of guild {0} to '{1}'", guild, prefix);
            _guildPrefixes[guild.Id] = prefix;
            await Redis.StoreStringAsync(Namespace.Combine(guild.Id, "prefix"), prefix);
        }

        #region Deinitialization

        /// <summary>
        /// Deinitializes the client
        /// </summary>
        /// <returns></returns>
        public async Task DeinitAsync()
        {
            await Discord.DisconnectAsync();
        }

        /// <summary>
        /// Disposes the client
        /// </summary>
        public void Dispose()
        {
            MessageCounter.Dispose();
            Redis.Dispose();
            Discord.Dispose();
        }
        #endregion

        /// <summary>
        /// Logs an exception.
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task LogException(Exception exception, DiscordMessage context = null)
        {
            Logger.LogError(exception);
            var hook = await Discord.GetWebhookAsync(Configuration.ErrorWebhook);
            await hook.ExecuteAsync("An error has occured on " + Discord.CurrentApplication.Name + ". ", embeds: new DiscordEmbed[] {
                exception.ToEmbed(),
                new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Orange,
                    Title = "Details",
                    Timestamp = DateTime.UtcNow
                }
                .AddField("Guild", context?.Channel.GuildId.ToString())
                .AddField("Channel", context?.Channel.Id.ToString())
                .AddField("Message", context?.Id.ToString())
            }, files: null);
        }
    }
}
