using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KoalaBot.Permissions
{
    public class Group
    {
        protected List<Permission> defines;
        public GuildManager Manager { get; }
        public string Name { get; }

        public Group(GuildManager manager, string name)
        {
            defines = new List<Permission>();
            Manager = manager;
            Name = name.ToLowerInvariant();
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
            if (permission.group)
            {
                //We need to make sure the group actually exists
                if (await Manager.GetGroupAsync(permission.name) == null)
                    throw new Exception("Cannot add permission because the group does not exist.");
            }

            //Add the define then save the group
            defines.Add(permission);

            //if we save, then do so
            if (save) await Manager.SaveGroupAsync(this);
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
        /// Deserializes a enumerable list of permissions.
        /// </summary>
        /// <param name="enumerable"></param>
        public void FromEnumerable(IEnumerable<string> enumerable)
        {
            foreach (string str in enumerable)
                defines.Add(Permission.FromString(str));
        }

        /// <summary>
        /// Serializes into an enumerable list of permissions.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> ToEnumerable()
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
                //We will scan and skip any groups that dont exist.
                if (!def.group || (await Manager.GetGroupAsync(def.name)) != null)
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

            return this;
        }

        /// <summary>
        /// Evaluates the permission for the group
        /// </summary>
        /// <param name="permission"></param>
        /// <returns></returns>
        public virtual async Task<State> EvaluateAsync(string permission)
        {
            //Prepare the state of the permission
            State state = State.Unset;

            //Continiously loop through the permissions, slowly getting smaller and smaller
            while (state == State.Unset && !string.IsNullOrEmpty(permission))
            {
                foreach (Permission perm in defines)
                {
                    //What happens if this is allow or deny :thonk:
                    if (perm.group)
                    {
                        //We are a group, so we need to get the group from memory
                        var group = await this.Manager.GetGroupAsync(perm.name);
                        if (group != null)
                        {
                            //We found the group, so we will now evaluate what it had and if its not unset we will use that.
                            var gstate = await group.EvaluateAsync(permission);
                            if (gstate != State.Unset) state = gstate;
                        }
                    }
                    else if (perm.name.Equals(permission))
                    {
                        //This permission matches, if its not unset then we will apply it
                        if (perm.state != State.Unset)
                            state = perm.state;
                    }
                }

                //We havn't found a state yet, so we will shrink it now
                if (state == State.Unset)
                {
                    //Get the permission of the dot. If there isnt any, break early.
                    int dot = permission.LastIndexOf('.');
                    if (dot <= 0) break;

                    //Trim upto the dot
                    permission = permission.Substring(0, dot - 1);
                }
            }

            return state;
        }
    }
}
