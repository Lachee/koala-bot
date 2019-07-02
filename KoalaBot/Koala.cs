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
using KoalaBot.Permissions.CommandNext;
using DSharpPlus.CommandsNext.Exceptions;
using System.Linq;
using KoalaBot.Ticker;
using KoalaBot.Database;

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
        public DatabaseClient Database { get; }
        public TickerManager TickerManager { get; }
        public MessageCounter MessageCounter { get; }
        public ModerationManager ModerationManager { get; }
        public PermissionManager PermissionManager { get; }
        public ReplyManager ReplyManager { get; }


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
            GuildSettings.DefaultPrefix = config.Prefix;

            Logger.Log("Creating new Database Client");
            this.Database = new DatabaseClient(config.SQL.Address, config.SQL.Database, config.SQL.Username, config.SQL.Password, config.SQL.Prefix, logger: Logger.CreateChild("DB"));

            //Configure Discord
            Logger.Log("Creating new Bot Configuration");
            this.Discord = new DiscordClient(new DiscordConfiguration() { Token = config.Token });

            //Make sure the user isn't updating to bypass moderative actions
            Logger.Log("Creating Instances....");
            ModerationManager = new ModerationManager(this);
            PermissionManager = new PermissionManager(this, Logger.CreateChild("PERM"));
            ReplyManager = new ReplyManager(this, Logger.CreateChild("REPLY"));
            TickerManager = new TickerManager(this, Logger.CreateChild("TICKER")) { Interval = 120 * 1000 };
            TickerManager.RegisterTickers(new ITickable[]
            {
                new TickerMessage("KoalaOS"),
                new TickerRandom(),
                new TickerGuildsAvailable(),
                new TickerMessageCount(),
                new TickerPermissionsCount()
            });

            //Track how many messages are sent
            Logger.Log("Creating Message Counter");
            this.MessageCounter = new MessageCounter(this, config.MessageCounterSyncRate * 1000);
          
            //Setup some deps
            Logger.Log("Creating Dependencies & Registering Commands");
            var deps = new ServiceCollection()
                .AddSingleton(this)
                .BuildServiceProvider(true);
            this.CommandsNext = this.Discord.UseCommandsNext(new CommandsNextConfiguration() { PrefixResolver = ResolvePrefix, Services = deps });
            this.CommandsNext.RegisterConverter(new PermissionGroupConverter());
            this.CommandsNext.RegisterConverter(new PermissionMemberGroupConverter());
            this.CommandsNext.RegisterCommands(Assembly.GetExecutingAssembly());
            
            //Catch when any errors occur in the command handler
            //Send any command errors back after logging it.
            Logger.Log("Registering Error Listeners");
            this.Discord.ClientErrored += async (error) => await LogException(error.Exception);
            this.CommandsNext.CommandErrored += HandleCommandErrorAsync;

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
        public async Task<int> ResolvePrefix(DiscordMessage message)
        {
            try
            {
                if (message.Author == null || message.Author.IsBot) return -1;

                //Get the prefix. If we fail to find the prefix then we will get it from the cache
                //if (!_guildPrefixes.TryGetValue(message.Channel.GuildId, out prefix))
                //{
                //    Logger.Log("Prefix Cache Miss. Fetching new prefix for guild " + message.Channel.GuildId);
                //    prefix = await Redis.FetchStringAsync(Namespace.Combine(message.Channel.GuildId, "prefix"), Configuration.Prefix);
                //    await UpdatePrefix(message.Channel.Guild, prefix);
                //}

                //Get the position of the prefix
                string prefix = await GuildSettings.GetPrefixAsync(message.Channel.Guild);
                var pos = message.GetStringPrefixLength(prefix);
                if (pos >= 0)
                {
                    //Make sure we are allowed to execute in this channel
                    // We want to be able to execute in this channel unless specifically denied.
                    var member = await message.Channel.Guild.GetMemberAsync(message.Author.Id);
                    var state = await member.HasPermissionAsync($"koala.execute.{message.ChannelId}", bypassAdmin: true, allowUnset: true);
                    if (!state) return -1;
                }

                //Return the index of the prefix
                return pos;
            }
            catch(Exception e)
            {
                this.Logger.LogError(e);
                return -1;
            }
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
            Database.Dispose();
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


        private async Task HandleCommandErrorAsync(CommandErrorEventArgs e)
        {
            //Log the exception
            Logger.LogError(e.Exception);

            //Check if we have permission
            if (e.Exception is ChecksFailedException cfe)
            {
                var first = cfe.FailedChecks.FirstOrDefault();
                if (first is PermissionAttribute pcfe)
                {
                    //We will be silent about missing permissions unless we are admin rank
                    if (e.Context.Member.Roles.Any(role => role.Permissions.HasPermission(DSharpPlus.Permissions.ManageRoles | DSharpPlus.Permissions.Administrator)))
                        await e.Context.RespondAsync($"You require the `{pcfe.Permission}` permission to use this command.");
                    return;
                }
                else
                {
                    //Generic bad permissions
                    await e.Context.ReplyExceptionAsync($"You failed the check {first.GetType().Name} and cannot execute the function.");
                    return;
                }
            }

            //The bot itself is unable to do it.
            if (e.Exception is DSharpPlus.Exceptions.UnauthorizedException)
            {
                var trace = e.Exception.StackTrace.Split(" in ", 2)[0].Trim().Substring(3);
                await e.Context.RespondAsync($"I do not have permission to do that, sorry.\n`{trace}`");
                return;
            }

            //We dont know the command, so just skip
            if (e.Exception is DSharpPlus.CommandsNext.Exceptions.CommandNotFoundException)
                return;

            //If all else fails, then we will log it
            await e.Context.ReplyExceptionAsync(e.Exception, false);
        }
    }
}
