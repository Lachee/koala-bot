using KoalaBot.Redis.Serialize;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace KoalaBot.Redis
{


    public class StackExchangeConnection : IRedisConnection
    {
        public IDatabaseAsync DatabaseAsync { get; protected set; }
        public StackExchangeConnection(IDatabaseAsync db)
        {
            DatabaseAsync = db;
        }

        #region util
        public async Task SetExpiryAsync(string key, TimeSpan ttl)
        {
            await DatabaseAsync.KeyExpireAsync(key, ttl);
        }
        public async Task<bool> RemoveAsync(string key)
        {
            return await DatabaseAsync.KeyDeleteAsync(key);
        }
        public async Task<bool> ExistsAsync(string key)
        {
            return await DatabaseAsync.KeyExistsAsync(key);
        }

        /// <summary>
        /// Increments a integer field in a HashMap
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<long> IncrementAsync(string key, string field, long value = 1) => await DatabaseAsync.HashIncrementAsync(key, field, value);

        /// <summary>
        /// Increments a integer
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<long> IncrementAsync(string key, long value = 1) => await DatabaseAsync.StringIncrementAsync(key, value);

        /// <summary>
        /// Decrements a integer field in a HashMap
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<long> DecrementAsync(string key, string field, long value = 1) => await DatabaseAsync.HashDecrementAsync(key, field, value);

        /// <summary>
        /// Increments a integer
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<long> DecrementAsync(string key, long value = 1) => await DatabaseAsync.StringDecrementAsync(key, value);

        #endregion

        #region String (key value)
        public async Task StoreStringAsync(string key, string value, TimeSpan? TTL = null)
        {
            await DatabaseAsync.StringSetAsync(key, value, expiry: TTL);
        }
        public async Task<string> FetchStringAsync(string key, string @default = null)
        {
            var value = await DatabaseAsync.StringGetAsync(key);
            return value.HasValue ? value.ToString() : @default;
        }
        #endregion

        #region Hash (Dictionary)

        public async Task StoreStringAsync(string key, string field, string value) => await DatabaseAsync.HashSetAsync(key, field, value);
        public async Task StoreHashMapAsync(string key, Dictionary<string, string> values)
        {
            await DatabaseAsync.HashSetAsync(key, ConvertDictionary(values));
        }

        public async Task<string> FetchStringAsync(string key, string field, string @default = null)
        {
            var value = await DatabaseAsync.HashGetAsync(key, field);
            return value.HasValue ? value.ToString() : @default;
        }

        /// <summary>
        /// Gets a entire hash
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, string>> FetchHashMapAsync(string key)
        {
            var hashvals = await DatabaseAsync.HashGetAllAsync(key);
            return ConvertHashEntries(hashvals);
        }
        #endregion

        #region Object
        /// <summary>
        /// Serializes an object and stores it under a hash
        /// </summary>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task StoreObjectAsync(string key, object obj)
        {
            var dict = RedisConvert.Serialize(obj);
            if (dict.Keys.Count > 0)
            {
                await this.StoreHashMapAsync(key, dict);
            }
            else
            {
                //TODO: Delete the key maybe?
                Console.WriteLine("Attempted to write a null key!");
            }
        }

        /// <summary>
        /// Gets a hash and deserializes into a object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<T> FetchObjectAsync<T>(string key)
        {
            //Get the hashset
            var dict = await this.FetchHashMapAsync(key);
            return RedisConvert.Deserialize<T>(dict);
        }
        #endregion

        #region Set
        /// <summary>
        /// Adds a value to a set
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<long> AddHashSetAsync(string key, string value)
        {
            return await DatabaseAsync.SetAddAsync(key, value) ? 1 : 0;
        }

        /// <summary>
        /// Adds values to a set
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public async Task<long> AddHashSetAsync(string key, params string[] values)
        {
            RedisValue[] redisValues = new RedisValue[values.Length];
            for (int i = 0; i < values.Length; i++) redisValues[i] = values[i];
            return await DatabaseAsync.SetAddAsync(key, redisValues);
        }

        /// <summary>
        /// Adds values to a set
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public async Task<long> AddHashSetAsync(string key, HashSet<string> values)
        {
            //Prepare the current index and the holder of the values
            RedisValue[] redisValues = new RedisValue[values.Count];
            int current = 0;

            //Iterate over the hashset, adding the elements
            foreach (var v in values) redisValues[current++] = v;

            //Add the final result to the DB
            return await DatabaseAsync.SetAddAsync(key, redisValues);
        }

        /// <summary>
        /// Removes a value from the set
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<bool> RemoveHashSetAsync(string key, string value)
        {
            return await DatabaseAsync.SetRemoveAsync(key, value);
        }


        /// <summary>
        /// Gets all values in a set
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<HashSet<string>> FetchHashSetAsync(string key)
        {
            RedisValue[] redisValues = await DatabaseAsync.SetMembersAsync(key);
            return new HashSet<string>(redisValues.Select(rv => rv.ToString()));
        }

        /// <summary>
        /// Gets a random value in a set
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<string> FetchRandomHashSetElementAsync(string key)
        {
            return await DatabaseAsync.SetRandomMemberAsync(key);
        }
        #endregion

        #region List
        
        #endregion


        private HashEntry[] ConvertDictionary(Dictionary<string, string> dictionary)
        {
            int index = 0;
            HashEntry[] entries = new HashEntry[dictionary.Count];
            foreach (var kp in dictionary)
            {
                entries[index++] = new HashEntry(kp.Key, kp.Value);
            }

            return entries;
        }
        private Dictionary<string, string> ConvertHashEntries(HashEntry[] entries)
        {
            if (entries == null) return null;
            Dictionary<string, string> dict = new Dictionary<string, string>(entries.Length);
            foreach (var entry in entries) dict.Add(entry.Name, entry.Value);
            return dict;
        }

    }

    public class StackExchangeClient : StackExchangeConnection, IRedisClient
    {
        private ConnectionMultiplexer redis;
        public IDatabase Database { get; }
        public Logging.Logger Logger { get; }

        public StackExchangeClient(string host, int db, Logging.Logger logger = null) : base(null)
        {
            Logger = logger ?? new Logging.Logger("REDIS", null);

            Logger.Log("Connecting to Redis: {0}", host);
            redis = ConnectionMultiplexer.Connect(host );

            Logger.Log("Getting Database {0}", db);
            DatabaseAsync = Database = redis.GetDatabase(db);
        }

		/// <summary>
		/// Initializes the client
		/// </summary>
		/// <returns></returns>
		public async Task InitAsync() => await Task.Delay(0);
        
        /// <summary>
        /// Creates a transaction
        /// </summary>
        /// <returns></returns>
        public IRedisTransaction CreateTransaction()
        {
            return new StackExchangeTransaction(Database.CreateTransaction());
        }

        /// <summary>
        /// Disposes the redis client
        /// </summary>
        public void Dispose()
        {
            redis.Dispose();
        }
    }

    public class StackExchangeTransaction : StackExchangeConnection, IRedisTransaction
    {
        public ITransaction Transaction { get; }
        public StackExchangeTransaction(ITransaction transaction) : base(transaction)
        {
            Transaction = transaction;
        }

        public async Task ExecuteAsync()
        {
            await Transaction.ExecuteAsync();
        }
    }
}