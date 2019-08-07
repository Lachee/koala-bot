using DSharpPlus;
using DSharpPlus.Entities;
using KoalaBot.Logging;
using KoalaBot.Permissions.Events;
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
        
        public const string DEFAULT_GROUP = "group.default";
        public DiscordGuild Guild { get; }
        private Dictionary<string, Group> _groups;
        
        public Koala Bot { get; }
        public IRedisClient Redis => Bot.Redis;
        public Logger Logger { get; }

        public DateTime GroupListModifiedAt { get; private set; }


        #region events
        /// <summary>
        /// Fired when a group is saved
        /// </summary>
        public event AsyncEventHandler<GroupEventArgs> GroupSaved
        {
            add => this._groupSaved.Register(value);
            remove => this._groupSaved.Unregister(value);
        }
        private AsyncEvent<GroupEventArgs> _groupSaved;


        /// <summary>
        /// Fired when a group is loaded
        /// </summary>
        public event AsyncEventHandler<GroupEventArgs> GroupLoaded
        {
            add => this._groupLoaded.Register(value);
            remove => this._groupLoaded.Unregister(value);
        }
        private AsyncEvent<GroupEventArgs> _groupLoaded;

        /// <summary>
        /// Fired when a group is deleted
        /// </summary>
        public event AsyncEventHandler<GroupEventArgs> GroupDeleted
        {
            add => this._groupDeleted.Register(value);
            remove => this._groupDeleted.Unregister(value);
        }
        private AsyncEvent<GroupEventArgs> _groupDeleted;
        #endregion

        public GuildManager(Koala bot, DiscordGuild guild, Logger logger = null)
        {
            this._groups = new Dictionary<string, Group>();
            this.Guild = guild;
            this.Logger = logger ?? bot.Logger.CreateChild("PERM");
            this.Bot = bot;
            this.GroupListModifiedAt = DateTime.Now;

            this._groupSaved = new AsyncEvent<GroupEventArgs>(Logger.LogEventException, "GROUP_SAVED");
            this._groupLoaded = new AsyncEvent<GroupEventArgs>(Logger.LogEventException, "GROUP_LOADED");
            this._groupDeleted = new AsyncEvent<GroupEventArgs>(Logger.LogEventException, "GROUP_DELETED");
        }

        /// <summary>
        /// Clears all cached groups.
        /// </summary>
        public void Reload()
        {
            _groups.Clear();
            this.GroupListModifiedAt = DateTime.Now;
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
            GroupListModifiedAt = DateTime.Now;
            return newGroup;
        }
        
        /// <summary>
        /// Finds all the groups.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<string>> FindGroupsAsync()
        {
            string pattern = Namespace.Combine(Guild, "permissions", "*");
            var keys = await Task.Run(() =>
            {
                var redis = Redis as StackExchangeClient;
                return redis.GetServersEnumable().First().Keys(pattern: pattern).Select(rk => rk.ToString());
            });

            return keys.Select(k => k.Substring(pattern.Length - 1));
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
            await this._groupDeleted.InvokeAsync(new GroupEventArgs(this, group));
            UnloadGroup(group.Name.ToLowerInvariant());
        }

        /// <summary>
        /// Removes a group from the cache
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool UnloadGroup(string name)
        {
            GroupListModifiedAt = DateTime.Now;
            return _groups.Remove(name);
        }
            /// <summary>
            /// Kiads a group under this guild.
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            private async Task<Group> LoadGroupAsync(string name)
        {
            var map = await Redis.FetchHashMapAsync(Namespace.Combine(Guild, "permissions", name.ToLowerInvariant()));
            if (map == null || map.Count == 0) return null;
            var group = new Group(this, name);
            await group.DeserializeAsync(map);
            await this._groupLoaded.InvokeAsync(new GroupEventArgs(this, group));
            return group;
        }
        
        /// <summary>
        /// Get a member group
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private async Task<MemberGroup> LoadMemberGroupAsync(DiscordMember member)
        {
            string name = GetMemberGroupName(member);
            var map = await Redis.FetchHashMapAsync(Namespace.Combine(Guild, "permissions", name.ToLowerInvariant()));
            if (map == null || map.Count == 0) return null;
            var group = new MemberGroup(this, name, member);
            await group.DeserializeAsync(map);
            await this._groupLoaded.InvokeAsync(new GroupEventArgs(this, group));
            return group;
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
            GroupListModifiedAt = DateTime.Now;

            //Serialize and store
            var map = await group.SerializeAsync();
            await Redis.StoreHashMapAsync(Namespace.Combine(Guild, "permissions", group.Name), map);
            await this._groupSaved.InvokeAsync(new GroupEventArgs(this, group));
        }

        /// <summary>
        /// Gets a user group. If it doesnt exist, then it will be created.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<MemberGroup> GetMemberGroupAsync(DiscordMember member)
        {
            string name = GetMemberGroupName(member);
            if (_groups.TryGetValue(name, out var g))
                return (MemberGroup)g;

            //Load the member. If we fail to load, then create a new one
            var memg = await LoadMemberGroupAsync(member);
            if (memg == null)
            {
                memg = new MemberGroup(this, name, member);
            }

            //Add to the group list
            _groups.Add(name, memg);
            GroupListModifiedAt = DateTime.Now;
            return memg;
        }
        public string GetMemberGroupName(DiscordMember member) => $"group.user.{member.Id}";

        /// <summary>
        /// Gets the group for the specified role.
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public async Task<Group> GetRoleGroupAsync(DiscordRole role)
        {
            var group = await GetGroupAsync(GetRoleGroupName(role));
            if (group == null)
            {
                group = new Group(this, GetRoleGroupName(role));
                await SaveGroupAsync(group);
            }
            return group;
        }
        public string GetRoleGroupName(DiscordRole role) => $"group.role.{role.Id}";

        /// <summary>
        /// Removes a user group from the cache
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool UnloadUserGroup(DiscordUser user) => UnloadGroup($"group.user.{user.Id}");
    }
}
