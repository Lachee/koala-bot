using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KoalaBot.PermissionEngine.Groups;
using KoalaBot.Redis;

namespace KoalaBot.PermissionEngine.Store
{
    class RedisStore : IStore
    {
        const string PRIORITY_KEY = "__priority";

        public string RootNamespace { get; set; }
        public IRedisClient Redis { get; }

        public RedisStore(IRedisClient client, string rootNamespace)
        {
            this.Redis = client;
            this.RootNamespace = rootNamespace;
        }

        public async Task<Group> AddGroupAsync(Group group)
        {
            var map = group.ToDictionary();
            map.Add(PRIORITY_KEY, group.Priority.ToString());
            await Redis.StoreHashMapAsync(GetKey(group), map);
            return group;
        }

        public Task ClearCacheAsync() => Task.CompletedTask;

        public Task<bool> DeleteGroupAsync(Group group) => Redis.RemoveAsync(GetKey(group));

        public async Task<Group> GetGroupAsync(Engine engine, string name)
        {
            var dictionary = await Redis.FetchHashMapAsync(GetKey(name));
            if (dictionary.Count == 0) return null;

            var priority = Group.DEFAULT_PRIORITY;

            if (dictionary.TryGetValue(PRIORITY_KEY, out var pk))
            {
                priority = int.Parse(pk);
                dictionary.Remove(PRIORITY_KEY);
            }

            Group group = new Group(engine, name) { Priority = priority };
            group.FromDictionary(dictionary);
            return group;
        }

        public async Task<IEnumerable<Group>> GetGroupsEnumerableAsync(Engine engine)
        {
            if (!(Redis is StackExchangeClient))
                throw new Exception("The redis client must be a StackExchangeClient to utlise the SCAN.");

            var key = GetKey("*");

            var redis = Redis as StackExchangeClient;
            var server = redis.GetServersEnumable().First();
            var keys = server.Keys(pattern: key);

            List<Group> groups = new List<Group>();
            foreach(var k in keys)
            {
                string name = k.ToString().Substring(key.Length - 1, k.ToString().Length - key.Length + 1);
                groups.Add(await GetGroupAsync(engine, name));
            }

            return groups;
        }


        public async Task<bool> SaveGroupAsync(Group group)
        {
            await this.DeleteGroupAsync(group);
            await this.AddGroupAsync(group);
            return true;
        }

        private string GetKey(Group group) => GetKey(group.Name);
        private string GetKey(string group) => Namespace.Join(RootNamespace, group);
    }
}
