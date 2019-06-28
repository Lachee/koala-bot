using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Permissions.Events
{
    public class GroupEventArgs : AsyncEventArgs
    {
        public GuildManager Manager { get; }
        public Group Group { get; }
        public GroupEventArgs(GuildManager manager, Group group)
        {
            Manager = manager;
            Group = group;
        }
    }
}
