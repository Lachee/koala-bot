using DSharpPlus.Entities;
using KoalaBot.Logging;
using KoalaBot.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Permissions
{
    public class GuildManager
    {
        public DiscordGuild Guild { get; }
        private Dictionary<string, Group> _groups;
        
        public Koala Bot { get; }
        public IRedisClient Redis => Bot.Redis;
        public Logger Logger { get; }

        public GuildManager(Koala bot, DiscordGuild guild, Logger logger = null)
        {
            this._groups = new Dictionary<string, Group>();
            this.Guild = guild;
            this.Logger = logger ?? bot.Logger.CreateChild("PERM");
            this.Bot = bot;
        }

        /// <summary>
        /// Clears all cached groups.
        /// </summary>
        public void Reload()
        {
            _groups.Clear();
        }

        /// <summary>
        /// Gets a group by name. If the group does not exist, it will try to recache it.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<Group> GetGroupAsync(string name)
        {
            //We already have it cached
            if (_groups.TryGetValue(name, out var group))
                return group;

            //Fetch the map
            var newGroup = await LoadGroupAsync(name);
            if (newGroup == null) return null;

            //Recache and return
            _groups.Add(name, newGroup);
            return newGroup;
        }

        /// <summary>
        /// Creates a new group and saves it
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<Group> CreateGroupAsync(string name)
        {
            if (!name.StartsWith("group."))
                throw new ArgumentException("The group name must begin with 'group.'!");

            Group group = new Group(this, name);
            await SaveGroupAsync(group);
            return group;
        }

        /// <summary>
        /// Deletes a group
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public async Task DeleteGroupAsync(Group group)
        {
            if (group == null || !group.Name.StartsWith("group."))
                throw new ArgumentException("The group cannot be null!");

            await Redis.RemoveAsync(Namespace.Combine(Guild, "permissions", group.Name.ToLowerInvariant()));
            UnloadGroup(group.Name.ToLowerInvariant());
        }

        /// <summary>
        /// Removes a group from the cache
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool UnloadGroup(string name) => _groups.Remove(name);

        /// <summary>
        /// Kiads a group under this guild.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<Group> LoadGroupAsync(string name)
        {
            var map = await Redis.FetchHashMapAsync(Namespace.Combine(Guild, "permissions", name.ToLowerInvariant()));
            if (map == null || map.Count == 0) return null;
            var group = new Group(this, name);
            return await group.DeserializeAsync(map);
        }

        /// <summary>
        /// Saves a group under this guild.
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public async Task SaveGroupAsync(Group group)
        {
            //UPdate our link
            _groups[group.Name] = group;

            //Serialize and store
            var map = await group.SerializeAsync();
            await Redis.StoreHashMapAsync(Namespace.Combine(Guild, "permissions", group.Name), map);
        }

        /// <summary>
        /// Gets a user group. If it doesnt exist, then it will be created.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<Group> GetUserGroupAsync(DiscordMember member)
        {
            var group = (await GetGroupAsync($"group.user.{member.Id}")) as MemberGroup;
            if (group == null)
            {
                group = new MemberGroup(this, $"group.user.{member.Id}", member);
                await SaveGroupAsync(group);
            }

            return group;
        }

        /// <summary>
        /// Gets the group for the specified role.
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public async Task<Group> GetRoleGroupAsync(DiscordRole role)
        {
            var group = await GetGroupAsync($"group.role.{role.Id}");
            if (group == null)
            {
                group = new Group(this, $"group.role.{role.Id}");
                await SaveGroupAsync(group);
            }
            return group;
        }

        /// <summary>
        /// Removes a user group from the cache
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool UnloadUserGroup(DiscordUser user) => UnloadGroup($"group.user.{user.Id}");
    }
}
