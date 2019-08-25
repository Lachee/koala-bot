using KoalaBot.PermissionEngine.Groups;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.PermissionEngine.Store
{
    /// <summary>
    /// Group Store
    /// </summary>
    public interface IStore
    {
        /// <summary>
        /// Adds a group to the store
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        Task<Group> AddGroupAsync(Group group);

        /// <summary>
        /// Gets a group from teh  store
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<Group> GetGroupAsync(Engine engine, string name);

        /// <summary>
        /// Clears the local cache, if any.
        /// </summary>
        /// <returns></returns>
        Task ClearCacheAsync();

        /// <summary>
        /// Deletes a group from the store
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        Task<bool> DeleteGroupAsync(Group group);

        /// <summary>
        /// Updates and saves a group in the store.
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        Task<bool> SaveGroupAsync(Group group);

        /// <summary>
        /// Gets all the groups
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Group>> GetGroupsEnumerableAsync(Engine engine);
    }
}
