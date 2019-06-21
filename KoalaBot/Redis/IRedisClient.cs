using DSharpPlus.Entities;
using KoalaBot.Redis.Serialize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Redis
{
    /// <summary>
    /// Naming structure in the redis database.
    /// </summary>
	public class Namespace : Stack<string>
	{
        /// <summary>
        /// The character that seperates folders.
        /// </summary>
        public const char Seperator = ':';

        /// <summary>
        /// The root namespace
        /// </summary>
		public static string RootNamespace { get; set; } = "";

        /// <summary>
        /// Sets the root folder to the given path.
        /// </summary>
        /// <param name="folders"></param>
        public static void SetRoot(params string[] folders)
        {
            RootNamespace = "";
            RootNamespace = Combine(folders).Substring(1);
        }



        /// <summary>
        /// Combines a series of elements together to form a final namespace. Includes the root namespace.
        /// </summary>
        /// <param name="folders"></param>
        /// <returns></returns>
        public static string Combine(params string[] folders)
        {
            StringBuilder builder = new StringBuilder(RootNamespace);
            if (folders.Length > 0)
            {
                for (int i = 0; i < folders.Length; i++)
                    builder.Append(Seperator).Append(folders[i]);
            }
            return builder.ToString();
        }     
        
        /// <summary>
        /// Combines a series of elements together to form a final namespace. Includes the root namespace and takes into account for snowflake objects
        /// </summary>
        /// <param name="folders"></param>
        /// <returns></returns>
        public static string Combine(params object[] folders)
        {
            StringBuilder builder = new StringBuilder(RootNamespace);
            if (folders.Length > 0)
            {
                for (int i = 0; i < folders.Length; i++)
                {
                    if (folders[i] is SnowflakeObject)
                    {
                        var so = (SnowflakeObject)folders[i];
                        builder.Append(Seperator).Append(so.Id);
                    }
                    else
                    {
                        builder.Append(Seperator).Append(folders[i]);
                    }
                }
            }
            return builder.ToString();
        }


        /// <summary>
        /// Creates a completely blank namespace.
        /// </summary>
        public Namespace() : base() { }

        /// <summary>
        /// Creates a new instance of the name space with initial folders.
        /// </summary>
        /// <param name="folders"></param>
        public Namespace(IEnumerable<string> folders) : base(folders) { }

        /// <summary>
        /// Creates a new isntance of the name space with initial folders.
        /// </summary>
        /// <param name="folders"></param>
        public Namespace(params object[] folders) : this(folders.Select(f => f.ToString())) { }

        /// <summary>
        /// Adds a element to the namespace
        /// </summary>
        /// <param name="folders">An array of folders to add</param>
        /// <returns></returns>
        public Namespace Add(params string[] folders)
        {
            for (int i = 0; i < folders.Length; i++) this.Push(folders[i]);
            return this;
        }

        /// <summary>
        /// Creates a key representation of the namespace
        /// </summary>
        /// <returns></returns>
        public string Build()
        {
            return Combine(this.Reverse().ToArray());
        }

        public static implicit operator string(Namespace name) { return name.Build(); }
	}

    public interface IRedisClient : IRedisConnection, IDisposable
    {
        /// <summary>
        /// Establishes and prepares the connection to the database
        /// </summary>
        /// <returns></returns>
        Task InitAsync();

        /// <summary>
        /// Creates a transaction
        /// </summary>
        /// <returns></returns>
        IRedisTransaction CreateTransaction();
    }

    public interface IRedisConnection
    {

        /// <summary>
        /// Stores a string
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">The contents</param>
        /// <param name="TTL">The time until the key expires</param>
        /// <returns></returns>
		Task StoreStringAsync(string key, string value, TimeSpan? TTL = null);

        /// <summary>
        /// Stores a value into the HashMap field.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task StoreStringAsync(string key, string field, string value);


        /// <summary>
        /// Fetches a string
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="default">The default value to return if the key does not exist.</param>
        /// <returns></returns>
        Task<string> FetchStringAsync(string key, string @default = null);

        /// <summary>
        /// Fetches a string from a hashmap
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="default">The default value to return if the key does not exist.</param>
        /// <returns></returns>
		Task<string> FetchStringAsync(string key, string field, string @default = null);

        /// <summary>
        /// Increments the integer value of a key
        /// </summary>
        /// <param name="key">The key to increment</param>
        /// <param name="value">The value to increment by.</param>
        /// <returns></returns>
        Task<long> IncrementAsync(string key, long value = 1);

        /// <summary>
        /// Increments the integer value of a field in a hashmap
        /// </summary>
        /// <param name="key">The key of the hashmap</param>
        /// <param name="field">The field to increment</param>
        /// <param name="value">The value to increment by.</param>
        /// <returns></returns>
        Task<long> IncrementAsync(string key, string field, long value = 1);

        /// <summary>
        /// Decrements the integer value of a key
        /// </summary>
        /// <param name="key">The key to decrement</param>
        /// <param name="value">The value to decrement by.</param>
        /// <returns></returns>
        Task<long> DecrementAsync(string key, long value = 1);

        /// <summary>
        /// Decrements the integer value of a field in a hashmap
        /// </summary>
        /// <param name="key">The key of the hashmap</param>
        /// <param name="field">The field to decrement</param>
        /// <param name="value">The value ot decrement by</param>
        /// <returns></returns>
        Task<long> DecrementAsync(string key, string field, long value = 1);

        /// <summary>
        /// Stores a HashMap into the dictionary
        /// </summary>
        /// <param name="key">The key to store it as</param>
        /// <param name="values">The hashmap to store</param>
        /// <returns></returns>
        Task StoreHashMapAsync(string key, Dictionary<string, string> values);


        /// <summary>
        /// Fetches a HashMap
        /// </summary>
        /// <param name="key">The key of the HashMap</param>
        /// <returns></returns>
        Task<Dictionary<string, string>> FetchHashMapAsync(string key);

        /// <summary>
        /// Stores a object using simplified serialization as a HashMap. 
        /// <para>This only supports very simplistic classes with no nestled classes, no generics, no arrays and no custom serialization available.</para>
        /// <para>For more advance serialization, see <see cref="RedisConvert.WriteAsync{T}(Namespace, T, TimeSpan?)"/></para>
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="obj">The object to store</param>
        /// <returns></returns>
        Task StoreObjectAsync(string key, object obj);

        /// <summary>
        /// Fetches a object using simplified serialization from a HashMap.
        /// <para>This only supports very simplistic classes with no nestled classes, no generics, no arrays and no custom serialization available.</para>
        /// <para>For more advance serialization, see <see cref="RedisConvert.WriteAsync{T}(Namespace, T, TimeSpan?)"/></para>
        /// </summary>
        /// <typeparam name="T">The type of object to fetch.</typeparam>
        /// <param name="key">The key of the hashmap</param>
        /// <returns></returns>
        Task<T> FetchObjectAsync<T>(string key);

        /// <summary>
        /// Adds a value to a HashSet
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task<long> AddHashSetAsync(string key, string value);

        /// <summary>
        /// Adds a range of values to the HashSet
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        Task<long> AddHashSetAsync(string key, params string[] values);

        /// <summary>
        /// Unions a HashSet with the stored HashSet
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        Task<long> AddHashSetAsync(string key, HashSet<string> values);

        /// <summary>
        /// Removes a value from the HashSet
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task<bool> RemoveHashSetAsync(string key, string value);

        /// <summary>
        /// Fetches a random element from the HashSet
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<string> FetchRandomHashSetElementAsync(string key);

        /// <summary>
        /// Fetches the entire HashSet
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<HashSet<string>> FetchHashSetAsync(string key);
        
        /// <summary>
        /// Sets the expiry of a key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="ttl"></param>
        /// <returns></returns>
        Task SetExpiryAsync(string key, TimeSpan ttl);
        
        /// <summary>
        /// Removes a key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<bool> RemoveAsync(string key);

        /// <summary>
        /// Checks if the key exists.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<bool> ExistsAsync(string key);
    }

    public interface IRedisTransaction : IRedisConnection
    {
        /// <summary>
        /// Executes the transaction
        /// </summary>
        /// <returns></returns>
        Task ExecuteAsync();
    }
}
