using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using KoalaBot.PermissionEngine.Groups;

namespace KoalaBot.PermissionEngine
{
    public struct Permission
    {
        public const string GROUP_PREFIX = "group.";

        /// <summary>
        /// State of the permission
        /// </summary>
        public StateType State { get; set; }

        /// <summary>
        /// Name of the permission
        /// </summary>
        public string Name { get => _name; set => _name = CleanName(value); }

        private string _name;

        public bool IsGroup => _name.StartsWith(GROUP_PREFIX);
        public string GroupName => _name.Substring(GROUP_PREFIX.Length);

        public Permission(string name) : this(name, StateType.Unset) { }
        public Permission(string name, StateType state)
        {
            _name = CleanName(name);
            State = state;
            Record(name);
        }

        public static Permission FromGroup(Groups.Group group, StateType state) => FromGroup(group.Name, state);

        public static Permission FromGroup(string groupName, StateType state)
        {
            return FromString(GROUP_PREFIX + groupName, state);
        }

        public static Permission FromString(string str, StateType defaultState = StateType.Unset)
        {
            if (str.Length <= 1)
                return new Permission(str, defaultState);
            
            //Check what the prefix is
            char s = str[0];
            switch (s)
            {
                case '+':
                    return new Permission() { Name = str.Substring(1), State = StateType.Allow };

                case '-':
                    return new Permission() { Name = str.Substring(1), State = StateType.Deny };

                case '?':
                    return new Permission() { Name = str.Substring(1), State = StateType.Unset };

                default:
                    return new Permission() { Name = str, State = defaultState };
            }
        }

        public override string ToString()
        {
            switch(State)
            {
                default:
                    return Name;

                case StateType.Unset:
                    return $"?{Name}";

                case StateType.Allow:
                    return $"+{Name}";

                case StateType.Deny:
                    return $"-{Name}";
            }
        }

        private static string CleanName(string name)
        {
            return Regex.Replace(name.ToLowerInvariant(), @"[\+\-:\s\|]", "").Trim();
        }

        private static HashSet<string> _allperms = new HashSet<string>();

        /// <summary>
        /// Number of permissions recorded
        /// </summary>
        public static int Recorded => _allperms.Count;

        /// <summary>
        /// List of recorded permissions
        /// </summary>
        public static IReadOnlyCollection<string> RecordedPermissions => _allperms;

        /// <summary>
        /// Records the permission.
        /// </summary>
        /// <param name="permission"></param>
        public static void Record(string permission)
        {
            _allperms.Add(permission);
        }

        /// <summary>
        /// Records the name of the permission.
        /// </summary>
        /// <param name="permission"></param>
        public static void Record(Permission permission) => Record(permission.Name);
    }
}
