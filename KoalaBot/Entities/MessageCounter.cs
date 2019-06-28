using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using KoalaBot.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace KoalaBot.Entities
{
    public class MessageCounter : IDisposable
    {
        public Koala Bot { get; }
        public IRedisClient Redis => Bot.Redis;
        public double SyncRate { get; }

        private SemaphoreSlim _semaphore;
        private System.Timers.Timer _syncTimer;

        public event AsyncEventHandler<ChangesSyncedEventArgs> ChangesSynced;
        public class ChangesSyncedEventArgs : AsyncEventArgs
        {
            public IReadOnlyCollection<ulong> UpdatedUsers { get; }
            public IReadOnlyCollection<ulong> UpdatedGuilds { get; }
            public ChangesSyncedEventArgs(HashSet<ulong> users, HashSet<ulong> guilds)
            {
                UpdatedUsers = users;
                UpdatedGuilds = guilds;
            }
        }

        private Dictionary<ulong, GuildTracking> _userTallies;       
        private class GuildTracking : Dictionary<ulong, Tracking>
        {
            public int Increment(ulong guildID, int value = 1)
            {
                Tracking track;
                if (!TryGetValue(guildID, out track)) track.tally = 0;

                track.time = DateTime.UtcNow;
                track.tally += value;

                return (this[guildID] = track).tally;
            }
        }
        private struct Tracking
        {
            public DateTime? time;
            public int tally;
        }

        public MessageCounter(Koala bot, double syncTimeMillis)
        {
            this.Bot = bot;
            this.SyncRate = syncTimeMillis;

            _userTallies = new Dictionary<ulong, GuildTracking>();
            _semaphore = new SemaphoreSlim(1, 1);
            _syncTimer = new System.Timers.Timer(syncTimeMillis) { AutoReset = true };
            _syncTimer.Elapsed += async (sender, args) => await SyncChanges();
            _syncTimer.Start();

            bot.Discord.MessageCreated += async (args) =>
            {
                if (args.Author.IsBot) return;
                await RecordMessage(args.Message);
            };
        }

        public async Task RecordMessage(DiscordMessage message) => await RecordMessage(message.Author.Id, message.Channel.GuildId);
        private async Task RecordMessage(ulong userId, ulong guildId)
        {   
            //Sync the semaphore
            await _semaphore.WaitAsync();
            try
            {
                //Get the guild
                GuildTracking guildTracking;
                if (!_userTallies.TryGetValue(userId, out guildTracking))
                    guildTracking = new GuildTracking();

                //Update the value
                guildTracking.Increment(guildId);
                _userTallies[userId] = guildTracking;
            }
            catch(Exception e)
            {
                throw e;
            }
            finally
            {
                //We are finally done so release it.
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets the global counts of messages
        /// </summary>
        /// <returns></returns>
        public async Task<long> GetGlobalCountAsync()
        {
            string tallyString = await Redis.FetchStringAsync(Namespace.Combine("global", "posts"), "0");
            return long.Parse(tallyString);
        }

        /// <summary>
        /// Syncs the recent changes to the server.
        /// </summary>
        /// <returns></returns>
        public async Task SyncChanges()
        {
            //Sync the semaphore
            await _semaphore.WaitAsync();
            try
            {
                //Just skip if there is no maps
                if (_userTallies.Count == 0) return;

                //Prepare the transaction and put everything in
                HashSet<ulong> changedUsers = new HashSet<ulong>(_userTallies.Count);
                HashSet<ulong> changedGuilds = new HashSet<ulong>(Bot.Discord.Guilds.Count);
                long totalMessages = 0;

                var transaction = Redis.CreateTransaction();
                foreach (var userTally in _userTallies)
                {
                    ulong userId = userTally.Key;
                    changedUsers.Add(userId);

                    foreach (var guildTracking in userTally.Value)
                    {
                        changedGuilds.Add(guildTracking.Key);
                        totalMessages += guildTracking.Value.tally;
                        _ = transaction.IncrementAsync(Namespace.Combine(guildTracking.Key, userId, "posts"), value: guildTracking.Value.tally);
                        _ = transaction.IncrementAsync(Namespace.Combine("global", userId, "posts"), value: guildTracking.Value.tally);

                        if (guildTracking.Value.time.HasValue)
                            _ = transaction.StoreStringAsync(Namespace.Combine(guildTracking.Key, userId, "activity"), guildTracking.Value.time.Value.ToOADate().ToString());

                    }
                }

                //Add the total tally
                _ = transaction.IncrementAsync(Namespace.Combine("global", "posts"), totalMessages);

                //Execute the transaction
                await transaction.ExecuteAsync();
                if (ChangesSynced != null)
                    await ChangesSynced?.Invoke(new ChangesSyncedEventArgs(changedUsers, changedGuilds));
                
                //Clear the tallies
                _userTallies.Clear();
            }
            catch(Exception e)
            {
                await Bot.LogException(e);
            }
            finally
            {
                //We are finally done so release it.
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            _syncTimer.Dispose();
            _semaphore.Dispose();
        }
    }

}
