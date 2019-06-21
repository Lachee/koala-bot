using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Permissions
{
    public struct Permission
    {
        public bool group;
        public string name;
        public State state;

        public Permission(string name, State state = State.Unset)
        {
            this.group = name.StartsWith("group.");
            this.name = name.ToLowerInvariant();
            this.state = state;
        }

        /// <summary>
        /// Serializes the permission into the format of +name, -name or just name
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return (state == State.Allow ? "+" : state == State.Deny ? "-" : "?") + name;
        }

        public static Permission FromString(string str)
        {
            if (str[0] == '-')
                return new Permission(str.Substring(1), State.Deny);

            if (str[0] == '+')
                return new Permission(str.Substring(1), State.Allow);

            if (str[0] == '?')
                return new Permission(str.Substring(1), State.Unset);

            return new Permission(str, State.Allow);
        }
    }
}
