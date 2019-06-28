using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Permissions
{
    public struct Permission
    {
        /// <summary>
        /// A collection of all permissions that have been used and recorded. Mainly used for the `\perm all` command to see a list of available permissions.
        /// </summary>
        public static IReadOnlyCollection<string> Recorded { get => _permcache; }
        private static HashSet<string> _permcache;

        /// <summary>
        /// Records a permisison in our list of available permissions.
        /// </summary>
        /// <param name="permission"></param>
        /// <returns></returns>
        public static bool Record(string permission) => _permcache.Add(permission);

        static Permission()
        {
            _permcache = new HashSet<string>();
        }


        public bool isGroup;
        public bool isRole;
        public bool isMember;
        public string name;
        public State state;

        public Permission(string name, State state = State.Unset)
        {
            this.isGroup = name.StartsWith("group.");
            this.isRole = name.StartsWith("group.role");
            this.isMember = name.StartsWith("group.user");
            this.name = name.ToLowerInvariant();
            this.state = state;
        }

        /// <summary>
        /// Serializes the permission into the format of `+name`, `-name`, or `?name`
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return (state == State.Allow ? "+" : state == State.Deny ? "-" : "?") + name;
        }

        /// <summary>
        /// Deserialzies a string into a permission, using the format of `+name`, `-name`, `?name`, or just `name`
        /// </summary>
        /// <param name="str">The string to parse</param>
        /// <param name="default">The default state</param>
        /// <returns></returns>
        public static Permission FromString(string str, State @default = State.Allow)
        {
            if (str[0] == '-')
                return new Permission(str.Substring(1), State.Deny);

            if (str[0] == '+')
                return new Permission(str.Substring(1), State.Allow);

            if (str[0] == '?')
                return new Permission(str.Substring(1), State.Unset);

            return new Permission(str, @default);
        }
    }
}
