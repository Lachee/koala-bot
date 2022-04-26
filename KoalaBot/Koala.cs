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

using DSharpPlus.CommandsNext.Exceptions;
using System.Linq;
using KoalaBot.Ticker;
using KoalaBot.Database;
using KoalaBot.Starwatch;
using KoalaBot.CommandNext;
using DSharpPlus.Interactivity;
using KoalaBot.Managers;
using KoalaBot.Exceptions;
using System.Net.Http;

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

        public InteractivityExtension Interactivity { get; }
        public CommandsNextExtension CommandsNext { get; }

        public Logger Logger { get; }
        public IRedisClient Redis { get; }
        public DbContext DbContext { get; }
        public TickerManager TickerManager { get; }
        public MessageCounter MessageCounter { get; }
        public ModerationManager ModerationManager { get; }
        public PermissionManager PermissionManager { get; }
        public ReplyManager ReplyManager { get; }
        public ReactRoleManager ReactRoleManager { get; }
        public StarwatchClient Starwatch { get; }

        //public System.Net.Http.HttpC

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
            this.DbContext = new DbContext(config.SQL, logger: Logger.CreateChild("DB"));

            Logger.Log("Creating Starwatch Client");
            this.Starwatch = new StarwatchClient(config.Starwatch.Host, config.Starwatch.Username, config.Starwatch.Password);

            //Configure Discord
            Logger.Log("Creating new Bot Configuration");
            this.Discord = new DiscordClient(new DiscordConfiguration() { Token = config.Token });

            //Make sure the user isn't updating to bypass moderative actions
            Logger.Log("Creating Instances....");
            ModerationManager = new ModerationManager(this);
            PermissionManager = new PermissionManager(this, Logger.CreateChild("PERM"));
            ReplyManager = new ReplyManager(this, Logger.CreateChild("REPLY"));
            ReactRoleManager = new ReactRoleManager(this, Logger.CreateChild("ROLE"));
            TickerManager = new TickerManager(this, Logger.CreateChild("TICKER")) { Interval = 120 * 1000 };
            TickerManager.RegisterTickers(new ITickable[]
            {
                new TickerStarwatch(Starwatch),
                new TickerMessageCount(),
                new TickerStarwatch(Starwatch),
                new TickerRandom(),
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
            this.CommandsNext.RegisterConverter(new PermissionArgumentConverter());
            this.CommandsNext.RegisterConverter(new MemberPermissionArgumentConverter());
            this.CommandsNext.RegisterConverter(new QueryConverter());
            this.CommandsNext.RegisterConverter(new CommandQueryArgumentConverter());
            this.CommandsNext.RegisterConverter(new Starwatch.CommandNext.WorldConverter(this));

            var curr = Assembly.GetExecutingAssembly();
            var part = Assembly.GetAssembly(typeof(Modules.Starwatch.StarwatchModule.ProtectionModule));
            this.CommandsNext.RegisterCommands(part);
            this.CommandsNext.CommandExecuted += HandleCommandExecuteAsync;

            Logger.Log("Creating Interactivity");
            this.Interactivity = this.Discord.UseInteractivity(new InteractivityConfiguration()
            {
                PaginationBehaviour = DSharpPlus.Interactivity.Enums.PaginationBehaviour.Ignore,
                PaginationDeletion = DSharpPlus.Interactivity.Enums.PaginationDeletion.DeleteEmojis
            });

            //Catch when any errors occur in the command handler
            //Send any command errors back after logging it.
            Logger.Log("Registering Error Listeners");
            this.Discord.ClientErrored += async (client, error) => await LogException(error.Exception);
            this.CommandsNext.CommandErrored += HandleCommandErrorAsync;
            
            Logger.Log("Done");
        }


        /// <summary>Initializes the bot</summary>
        /// <returns></returns>
        public async Task InitAsync()
        {
            Logger.Log("Initializing Redis");
            await Redis.InitAsync();

            Logger.Log("Initializing DB");
            if (!string.IsNullOrEmpty(Configuration.SQL.DefaultImport))
            {
                Logger.Log("Importing Default DB");
                await DbContext.ImportSqlAsync(System.IO.Path.Combine(Configuration.Resources, Configuration.SQL.DefaultImport));
            }

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
            DbContext.Dispose();
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
            DiscordWebhookBuilder builder = new DiscordWebhookBuilder();

            builder.Content = $"An error has occurred on {Discord.CurrentApplication.Name}.";

            builder.AddEmbeds(new DiscordEmbed[] {
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
            });

            await hook.ExecuteAsync(builder);
        }


        /// <summary>
        /// Handles Command Executions.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task HandleCommandExecuteAsync(CommandsNextExtension ext, CommandExecutionEventArgs e)
        {
            //Save the execution to the database
            var cmdlog = new CommandLog(e.Context, failure: null);
            await cmdlog.SaveAsync(DbContext);
        }

        /// <summary>
        /// Handles Failed Executions
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task HandleCommandErrorAsync(CommandsNextExtension ext, CommandErrorEventArgs e)
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
                        await e.Context.ReplyAsync($"You require the `{pcfe.PermissionName}` permission to use this command.");
                    else
                        await e.Context.ReplyReactionAsync("🙅");

                    //Save the execution to the database
                    await (new CommandLog(e.Context, failure: $"bad permission. Needs {pcfe.PermissionName}.")).SaveAsync(DbContext);
                    return;
                }
                else
                {
                    //Generic bad permissions
                    await e.Context.ReplyExceptionAsync($"You failed the check {first.GetType().Name} and cannot execute the function.");

                    //Save the execution to the database
                    await (new CommandLog(e.Context, failure: $"Failed {first.GetType().Name} check.")).SaveAsync(DbContext);
                    return;
                }
            }

            //Its a permission exception
            if (e.Exception is PermissionException pe)
            {
                //Save the execution to the database
                await e.Context.ReplyReactionAsync("🙅");
                await (new CommandLog(e.Context, failure: $"bad permission. Needs {pe.Permission}.")).SaveAsync(DbContext);
                return;
            }

            //The bot itself is unable to do it.
            if (e.Exception is DSharpPlus.Exceptions.UnauthorizedException)
            {
                var trace = e.Exception.StackTrace.Split(" in ", 2)[0].Trim().Substring(3);
                await e.Context.ReplyAsync($"I do not have permission to do that, sorry.\n`{trace}`");

                //Save the execution to the database
                await (new CommandLog(e.Context, failure: $"Unauthorized")).SaveAsync(DbContext);
                return;
            }

            //We dont know the command, so just skip
            if (e.Exception is DSharpPlus.CommandsNext.Exceptions.CommandNotFoundException)
            {
                //Save the execution to the database
                await (new CommandLog(e.Context, failure: $"Command Not Found")).SaveAsync(DbContext);
                return;
            }

            //If all else fails, then we will log it
            await e.Context.ReplyExceptionAsync(e.Exception, false);

            //Save the execution to the database
            await (new CommandLog(e.Context, failure: $"Exception: {e.Exception.Message}")).SaveAsync(DbContext);
            return;
        }
    }
}
