using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using KoalaBot.Logging;
using KoalaBot.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace KoalaBot.Ticker
{
    public class TickerManager : IDisposable
    {
        public Koala Bot { get; }
        
        public IRedisClient Redis => Bot.Redis;
        public DiscordClient Discord => Bot.Discord;
        public Logger Logger { get; }
        public double Interval { get => timer.Interval; set => timer.Interval = value; }

        private int _currentTicker = -1;
        private List<ITickable> _tickers;
        private Timer timer;
        private object _lock;

        public TickerManager(Koala bot, Logger logger = null) : this(bot, new ITickable[0], logger) { }
        public TickerManager(Koala bot, IEnumerable<ITickable> tickers, Logger logger = null)
        {
            Bot = bot;
            Logger = logger ?? new Logger("TICKER");
            _tickers = new List<ITickable>(tickers);

            _lock = new object();
            timer = new Timer() { AutoReset = true };
            timer.Elapsed += TimerElapsed;
            timer.Start();

            bot.Discord.Ready += async (args) => await Tick();
        }

        private async void TimerElapsed(object sender, ElapsedEventArgs e) => await Tick();

        /// <summary>
        /// Ticks the ticker and updates the activity
        /// </summary>
        /// <returns></returns>
        public async Task Tick()
        {
            //Increment the ticker
            _currentTicker = (_currentTicker + 1) % _tickers.Count;

            //Fetch the ticker
            ITickable ticker = null;
            lock (_lock) ticker = _tickers[_currentTicker];

            //Tick
            try
            {
                Logger.Log("Ticking Activity with {0}", ticker);
                var activity = await ticker.GetActivityAsync(this);
                await Discord.UpdateStatusAsync(activity);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        /// <summary>
        /// Registers a ticker
        /// </summary>
        /// <param name="ticker"></param>
        public void RegisterTicker(ITickable ticker) { lock(_lock) _tickers.Add(ticker); }

        /// <summary>
        /// Registers a collection of tickers
        /// </summary>
        /// <param name="tickers"></param>
        public void RegisterTickers(IEnumerable<ITickable> tickers) { lock (_lock) _tickers.AddRange(tickers); }

        public void Dispose() => timer.Dispose();
    }
}
