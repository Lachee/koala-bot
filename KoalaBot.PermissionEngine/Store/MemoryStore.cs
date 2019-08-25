using KoalaBot.PermissionEngine.Groups;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.PermissionEngine.Store
{
    public class MemoryStore : IStore
    {
        private Dictionary<string, Group> _groups;
        public MemoryStore() { _groups = new Dictionary<string, Group>(); }

        public Task<Group> AddGroupAsync(Group group)
        {
            _groups[group.Name] = group;
            return Task.FromResult(group);
        }

        public Task<bool> DeleteGroupAsync(Group group)
        {
            return Task.FromResult(_groups.Remove(group.Name));
        }

        public Task<Group> GetGroupAsync(Engine engine, string name)
        {
            Group group = null;
            _groups.TryGetValue(name.ToLowerInvariant(), out group);
            return Task.FromResult(group);
        }

        public Task<bool> SaveGroupAsync(Group group)
        {
            _groups[group.Name] = group;
            return Task.FromResult(true);
        }

        public Task ClearCacheAsync() { return Task.CompletedTask; }

        public Task<IEnumerable<Group>> GetGroupsEnumerableAsync(Engine engine)
        {
            return Task.FromResult((IEnumerable<Group>) _groups.Values);
        }
    }
}
