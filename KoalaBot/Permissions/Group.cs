using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KoalaBot.Permissions
{
    public class Group
    {
        public const bool DO_CACHE = false;

        protected Dictionary<string, State> cache;
        protected DateTime cacheLastModified;

        protected List<Permission> defines;

        public GuildManager Manager { get; }
        public string Name { get;  }
        
        public Group(GuildManager manager, string name, int cacheCapacity = 3)
        {
            if (DO_CACHE) cache = new Dictionary<string, State>(cacheCapacity);
            defines = new List<Permission>();
            cacheLastModified = DateTime.Now;
            Manager = manager;
            Name = name.ToLowerInvariant();
        }

        /// <summary>
        /// Copies defines and cache of the group
        /// </summary>
        /// <param name="group"></param>
        protected virtual void CopyGroup(Group group)
        {
            defines = new List<Permission>(group.defines);

            if (DO_CACHE) cache = new Dictionary<string, State>(group.cache);
            cacheLastModified = group.cacheLastModified;
        }


        /// <summary>
        /// Adds a permission to the group and saves the group
        /// </summary>
        /// <param name="permission"></param>
        /// <returns></returns>
        public async Task AddPermissionAsync(string permission, bool save = true) => await AddPermissionAsync(Permission.FromString(permission), save);

        /// <summary>
        /// Adds a permission to the group and saves the group
        /// </summary>
        /// <param name="permission"></param>
        /// <returns></returns>
        public async Task AddPermissionAsync(Permission permission, bool save = true)
        {
            //Its a group, so lets do some validitity checks
            if (permission.isGroup)
            {
                if (permission.isMember)
                    throw new Exception("Cannot add permission because groups may only have child groups, not members.");
                
                if (permission.isRole)
                {
                    //Group will always exist if its a role
                    //if (await Manager.GetRoleGroupAsync(permission.name) == null)
                    //    throw new Exception("Cannot add permission because groups may only have child groups, not roles.");
                }
                else
                {
                    //If this isnt a role group, we need to make sure it exists
                    if (await Manager.GetGroupAsync(permission.name) == null)
                        throw new Exception($"Cannot add permission because the group {permission.name} does not exist.");
                }
                
            }

            //Add the define then save the group
            defines.Add(permission);
            if (DO_CACHE) cache.Clear();

            //if we save, then do so
            if (save) await SaveAsync();
        }

        /// <summary>
        /// Removes a permission from the group.
        /// </summary>
        /// <param name="permission"></param>
        /// <param name="save"></param>
        /// <returns></returns>
        public async Task RemovePermissionAsync(Permission permission, bool save = true) => await RemovePermissionAsync(permission.name, save);

        /// <summary>
        /// Removes a permission from the group
        /// </summary>
        /// <param name="permission"></param>
        /// <param name="save"></param>
        /// <returns></returns>
        public async Task RemovePermissionAsync(string permission, bool save = true)
        {
            for (int i = 0; i < defines.Count; i++)
            {
                if (defines[i].name.Equals(permission, StringComparison.InvariantCultureIgnoreCase))
                {
                    defines.RemoveAt(i);
                    break;
                }
            }

            if (DO_CACHE) cache.Clear();
            if (save) await SaveAsync();
        }

        /// <summary>
        /// Saves the group
        /// </summary>
        /// <returns></returns>
        public async Task SaveAsync()
        {
            await Manager.SaveGroupAsync(this);
        }

        /// <summary>
        /// Deletes the group
        /// </summary>
        /// <returns></returns>
        public async Task DeleteAsync()
        {
            await Manager.DeleteGroupAsync(this);
        }

        /// <summary>
        /// All the permissions to check
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<Permission> GetPermissionEnumerable() => defines;

        /// <summary>
        /// Deserializes a enumerable list of permissions.
        /// </summary>
        /// <param name="enumerable"></param>
        public virtual void ImportEnumerable(IEnumerable<string> enumerable)
        {
            foreach (string str in enumerable.Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l)))
                defines.Add(Permission.FromString(str));

            if (DO_CACHE) cache.Clear();
        }

        /// <summary>
        /// Serializes into an enumerable list of permissions.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<string> ExportEnumerable()
        {
            foreach (Permission perm in defines)
                yield return perm.ToString();
        }

        /// <summary>
        /// Serializes into a dictionary, removing duplicated values
        /// </summary>
        /// <returns></returns>
        public async Task<Dictionary<string, string>> SerializeAsync()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (var def in defines)
            {
                //Skip members
                if (def.isMember)
                    continue;

                //If we are a group
                if (def.isGroup && !def.isRole)
                { 
                    //Skip because we have nothing
                    if (await Manager.GetGroupAsync(def.name) == null)
                        continue;
                }

                //Add to the serialize list
                dict[def.name] = def.state.ToString();
            }

            //Return the dictionary
            return dict;
        }

        /// <summary>
        /// Deserializes from a dictionary.
        /// </summary>
        /// <param name="dictionary"></param>
        public async Task<Group> DeserializeAsync(Dictionary<string, string> dictionary)
        {
            foreach (var kp in dictionary)
            {
                if (Enum.TryParse(typeof(State), kp.Value, true, out var state))
                {
                    //Prepare the permission
                    var perm = new Permission(kp.Key.ToLowerInvariant(), (State)state);

                    //Add it, but don't save
                    await this.AddPermissionAsync(perm, false);
                }
                else
                {
                    throw new InvalidCastException($"Failed to cast {kp.Value} into a valid state.");
                }
            }

            if (DO_CACHE) cache.Clear();
            return this;
        }

        /// <summary>
        /// Evaluates the permission for the group
        /// </summary>
        /// <param name="permission"></param>
        /// <returns></returns>
        public virtual async Task<State> EvaluatePermissionAsync(string permission)
        {
            //Validate the cache. If the managers group list has been modified since last time, we will clear it.
            if (DO_CACHE)
            {
                if (cacheLastModified < Manager.GroupListModifiedAt)
                {
                    cache.Clear();
                    cacheLastModified = DateTime.Now;
                }

                //Check the cache
                if (cache.TryGetValue(permission, out var s))
                    return s;
            }

            //Prepare the state of the permission
            State state = State.Unset;

            //Continiously loop through the permissions, slowly getting smaller and smaller
            string scan = permission;
            while (state == State.Unset && !string.IsNullOrEmpty(scan))
            {
                foreach (Permission perm in GetPermissionEnumerable())
                {
                    //If this permission matches what we want, set our state
                    if (perm.name.Equals(scan))
                    {
                        //This permission matches, if its not unset then we will apply it
                        if (perm.state != State.Unset)
                            state = perm.state;
                    }

                    //Otherwise, we are a group so we should check deeper.
                    else if (perm.isGroup)
                    {
                        //We are a group, so we need to get the group from memory
                        var group = await this.Manager.GetGroupAsync(perm.name);
                        if (group != null)
                        {          
                            //We found the group, so we will now evaluate what it had and if its not unset we will use that.
                            var gstate = await group.EvaluatePermissionAsync(scan);

                            //If we are denying this permission, we want to always deny this permission
                            if (gstate != State.Unset)
                                state = perm.state == State.Deny ? State.Deny : gstate;
                        }
                    }
                }

                //We havn't found a state yet, so we will shrink it now
                if (state == State.Unset)
                {
                    //Get the permission of the dot. If there isnt any, break early.
                    int dot = scan.LastIndexOf('.');
                    if (dot <= 0) break;

                    //Trim upto the dot
                    scan = scan.Substring(0, dot);
                }
            }

            //Add to the cache
            if (DO_CACHE)
            {
                cache.Add(permission, state);
                cacheLastModified = DateTime.Now;
            }
            return state;
        }
    
        /// <summary>
        /// Evaluates all the sub permissions for the parent.
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public async Task<List<Permission>> EvaluatePermissionChildrenAsync(string parent)
        {
            List<Permission> subs = new List<Permission>();
            await EvaluatePermissionChildrenNonAllocatingAsync(parent, subs);
            return subs;
        }

        /// <summary>
        /// Adds all the sub permissions to the given list
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="subs"></param>
        /// <returns></returns>
        protected virtual async Task EvaluatePermissionChildrenNonAllocatingAsync(string parent, List<Permission> subs)
        {
            foreach (Permission perm in GetPermissionEnumerable())
            {
                //Skip members
                if (perm.isMember)
                    continue;

                //What happens if this is allow or deny :thonk:
                if (perm.isGroup)
                {
                    //We are a group, so we need to get the group from memory
                    var group = await this.Manager.GetGroupAsync(perm.name);
                    if (group != null)
                    {
                        //We found the group, so we will now evaluate what it had and if its not unset we will use that.
                        await group.EvaluatePermissionChildrenNonAllocatingAsync(parent, subs);
                    }
                }

                //If we start with the parent, add us.
                if (perm.name.StartsWith(parent))
                {
                    //Add the permission to our list of subs
                    subs.Add(perm);
                }
            }
        }

        /// <summary>
        /// Evaluates a permission tree. Only used for debugging and <see cref="EvaluatePermissionAsync(string)"/> should be used to evaluate individual permissions.
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public virtual async Task<PermTree> EvaluatePermissionTree(PermTree tree = null)
        {
            //Create the tree if it doesnt exist
            if (tree == null)
                tree = new PermTree(this);

            //Make sure the group is valid.
            if (tree.Group != this)
                throw new ArgumentException("PermTree does not have the correct group.");

            //Add all our permissions and groups
            foreach (Permission perm in defines)
            {
                if (perm.isGroup)
                {
                    var c = await Manager.GetGroupAsync(perm.name);
                    if (c != null)
                    {
                        //Create a child and evaluate it
                        var child = tree.AddChild(c);
                        await c.EvaluatePermissionTree(child);
                    }
                    else
                    {
                        //We are missing the group, so add it like a perm anyways
                        tree.AddPermission(perm);
                    }
                }
                else
                {
                    //We are a regular permission
                    tree.AddPermission(perm);
                }
            }

            //Return the tree
            return tree;
        }
    }
}
