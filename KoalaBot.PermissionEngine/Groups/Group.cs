using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;

namespace KoalaBot.PermissionEngine.Groups
{
    public class Group
    {
        public const int DEFAULT_PRIORITY = 10;

        /// <summary>
        /// The name of the group.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Priority of the group. By default, it is 10.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// The engine the group belongs too.
        /// </summary>
        public Engine Engine { get; }

        /// <summary>
        /// Private list of permissions
        /// </summary>
        protected Dictionary<string, Permission> permissions;

        /// <summary>
        /// Creates a new group
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="name"></param>
        public Group(Engine engine, string name)
        {
            this.Name = name.ToLowerInvariant();
            this.Engine = engine;
            this.Priority = DEFAULT_PRIORITY;
            this.permissions = new Dictionary<string, Permission>();
        }

        /// <summary>
        /// Gets the permission enumerable
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<Permission> PermissionsEnumerable() => permissions.Values;

        /// <summary>
        /// Adds a permission to the group
        /// </summary>
        /// <param name="permission"></param>
        public virtual void AddPermission(Permission permission)
        {
            permissions[permission.Name] = permission;
        }

        /// <summary>
        /// Adds a permission to the group
        /// </summary>
        /// <param name="permission"></param>
        /// <param name="defaultState"></param>
        public void AddPermission(string permission, StateType defaultState = StateType.Allow) => AddPermission(Permission.FromString(permission, defaultState));

        /// <summary>
        /// Adds a sub-group to this group.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="state"></param>
        public void AddPermission(Group group, StateType state = StateType.Allow) => AddPermission(Permission.FromGroup(group, state));

        /// <summary>
        /// Removes a permission
        /// </summary>
        /// <param name="permission"></param>
        /// <returns></returns>
        public bool RemovePermission(string permission) => RemovePermission(Permission.FromString(permission));

        /// <summary>
        /// Removes a permission
        /// </summary>
        /// <param name="permission"></param>
        /// <returns></returns>
        public bool RemovePermission(Permission permission)
        {
            return permissions.Remove(permission.Name);
        }

        /// <summary>
        /// Converts the group into a dictionary
        /// </summary>
        /// <returns></returns>
        public virtual Dictionary<string, string> ToDictionary()
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            foreach (var p in PermissionsEnumerable()) dictionary.Add(p.Name, p.State.ToString());
            return dictionary;
        }

        /// <summary>
        /// Creates the group from the dictionary
        /// </summary>
        /// <param name="dictionary"></param>
        public virtual void FromDictionary(Dictionary<string, string> dictionary)
        {
            foreach (var kp in dictionary)
            {
                StateType state = (StateType) Enum.Parse(typeof(StateType), kp.Value);
                AddPermission(new Permission(kp.Key, state));
            }
        }
        
        /// <summary>
        /// Gets the permission
        /// </summary>
        /// <param name="permission"></param>
        /// <returns></returns>
        public virtual Permission GetPermission(string permission)
        {
            permission = permission.ToLowerInvariant();
            if (permissions.TryGetValue(permission, out var perm))
                return perm;

            foreach (var p in PermissionsEnumerable())
                if (p.Name.Equals(permission))
                    return p;

            return new Permission(permission, StateType.Unset);
        }

        public Permission GetPermission(Group group) => GetPermission(Permission.FromGroup(group, StateType.Unset).Name);

        /// <summary>
        /// Saves the current group
        /// </summary>
        /// <returns></returns>
        public Task<bool> SaveAsync() => Engine.Store.SaveGroupAsync(this);

        /// <summary>
        /// Deletes the current group.
        /// </summary>
        /// <returns></returns>
        public Task<bool> DeleteAsync() => Engine.DeleteGroupAsync(this);

        /// <summary>
        /// Evaluates the group permission
        /// </summary>
        /// <param name="permission"></param>
        /// <returns></returns>
        public async Task<StateType> EvaluatePermissionAsync(string permission)
        {
            //The permission we plan to check.
            string node = permission.ToLowerInvariant();

            StateType state = StateType.Unset;

            //Group permissions are evaluated last.
            List<Permission> checklast = new List<Permission>();
            bool isFirstLoop = true;

            while(!string.IsNullOrEmpty(node) && state == StateType.Unset)
            {
                Permission perm = Permission.FromString(node);

                //Evaluate all the permissions.
                foreach (Permission p in PermissionsEnumerable())
                {
                    if (p.IsGroup && perm.IsGroup && perm.Name.Equals(p.Name))
                    {
                        state = p.State;
                        break;
                    }
                    else if (isFirstLoop && p.IsGroup)
                    {
                        //If we are a group, we will deal with it later. We only want to add it in our first iteration.
                        checklast.Add(p);
                        continue;
                    }
                    else if (p.Name.Equals(node))
                    {
                        //If we match, then set our state, and abort this loop
                        state = p.State;
                        break;
                    }
                }

                //Unset our flag and trim the characters if we are still unset
                isFirstLoop = false;
                if (state == StateType.Unset)
                {
                    int index = node.LastIndexOf('.');
                    node = index <= 0 ? null : node.Substring(0, index);
                }
            }

            if (state == StateType.Unset)
            {
                //Prepare all the groups
                Dictionary<Permission, Group> groups = new Dictionary< Permission, Group>(checklast.Count);
                foreach (var perm in checklast)
                {
                    var group = await Engine.GetGroupAsync(perm);
                    if (group == null) continue;
                    //if (group == null) throw new Exception("Group does not exist: " + perm.GroupName);

                    groups.Add(perm, group);
                }

                //Iterate, checking each group.
                foreach(var kp in groups.OrderByDescending(kp => kp.Value.Priority))
                { 
                    var gstate = await kp.Value.EvaluatePermissionAsync(permission);
                    state = gstate != StateType.Unset && kp.Key.State == StateType.Deny ? StateType.Deny : gstate;

                    if (state != StateType.Unset)
                        break;
                }
            }

            return state;
        }

        /// <summary>
        /// Evaluates a pattern, fetching all the permissions and their values matching that value.
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Permission>> EvaluatePatternAsync(Regex pattern)
        {
            Dictionary<string, Permission> permissions = new Dictionary<string, Permission>();
            await InternalEvaluatePatternAsync(permissions, pattern);
            return permissions.Values;
        }

        private async Task InternalEvaluatePatternAsync(Dictionary<string, Permission> evals, Regex pattern)
        {
            //Prepare a list of permissions to check last.
            List<Permission> checklast = new List<Permission>();

            //Evaluate all the permissions.
            foreach (Permission p in PermissionsEnumerable())
            {
                //Does it match our pattern? if so, add it.
                if (pattern.IsMatch(p.Name))
                {
                    if (evals.TryGetValue(p.Name, out var p2) && p2.State != StateType.Unset)
                    {
                        //We are skipping because we already have it.
                        //p2.State = await EvaluatePermissionAsync(p.Name);
                        //evals[p.Name] = p2;
                    }
                    else
                    {
                        evals[p.Name] = p;
                    }
                }

                //If we are a group, we will subcheck it.
                if (p.IsGroup)
                    checklast.Add(p);
            }

            //Prepare all the groups
            Dictionary<Permission, Group> groups = new Dictionary<Permission, Group>(checklast.Count);
            foreach (var perm in checklast)
            {
                var group = await Engine.GetGroupAsync(perm);
                if (group == null) continue;

                groups.Add(perm, group);
            }

            //Iterate, checking each group.
            foreach (var kp in groups.OrderByDescending(kp => kp.Value.Priority))
                await kp.Value.InternalEvaluatePatternAsync(evals, pattern);
        }
    }
}