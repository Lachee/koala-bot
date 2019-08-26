using KoalaBot.PermissionEngine.Groups;
using KoalaBot.PermissionEngine.Store;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.PermissionEngine
{
    public class Engine
    {
        public IStore Store { get; }

        /// <summary>
        /// Creates a new permission engine with a <seealso cref="MemoryStore"/>
        /// </summary>
        public Engine() : this(new MemoryStore()) { }

        /// <summary>
        /// Creates a new permission engine with a specified <see cref="IStore"/>
        /// </summary>
        /// <param name="store"></param>
        public Engine(IStore store)
        {
            Store = store;
        }

        public async Task<string> ExportAsync()
        {
            StringBuilder builder = new StringBuilder();
            var groups = await Store.GetGroupsEnumerableAsync(this);

            foreach (var group in groups)
                builder.Append(ExportGroup(group));

            return builder.ToString();
        }

        public async Task ImportAsync(string import)
        {
            Group group = null;

            string[] lines = import.Split('\n');
            foreach(var rawLine in lines)
            {
                var line = rawLine.Split('#')[0].Trim();
                if (line.StartsWith("#") || string.IsNullOrEmpty(line)) continue;

                if (line.StartsWith("::"))
                {
                    if (group != null)
                        await AddGroupAsync(group);

                    var parts = line.Substring(2).Split('|');
                    group = new Group(this, parts[0]);

                    if (parts.Length > 1 && int.TryParse(parts[1], out var priority))
                        group.Priority = priority;
                }
                else
                {
                    group.AddPermission(Permission.FromString(line));
                }
            }

            if (group != null)
                await AddGroupAsync(group);
        }

        /// <summary>
        /// Exports a single group
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public string ExportGroup(Group group)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("::").Append(group.Name);
            if (group.Priority != Group.DEFAULT_PRIORITY)
                builder.Append("|").Append(group.Priority);
            builder.Append('\n');

            foreach (var permission in group.PermissionsEnumerable())
                builder.Append(permission.ToString()).Append('\n');

            return builder.ToString();
        }

        public Task<bool> DeleteGroupAsync(Group group) => Store.DeleteGroupAsync(group);

        public Task<Group> AddGroupAsync(Group group) => Store.AddGroupAsync(group);
        public Task<Group> AddGroupAsync(string name) => this.AddGroupAsync(new Group(this, name));

        public Task<Group> GetGroupAsync(string name) => Store.GetGroupAsync(this, name);

        internal Task<Group> GetGroupAsync(Permission permission)
        {
            if (!permission.IsGroup)
                throw new ArgumentException("Permission is not a group permission!", "permission");
            return GetGroupAsync(permission.GroupName);
        }

    }
}
