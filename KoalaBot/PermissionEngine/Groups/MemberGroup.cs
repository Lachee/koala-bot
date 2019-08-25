using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.PermissionEngine.Groups
{
    public class MemberGroup : Group
    {
        public DiscordMember Member { get; }


        public MemberGroup(Engine engine, DiscordMember member) : base(engine, GetGroupName(member))
        {
            Member = member;
        }

        public override IEnumerable<Permission> PermissionsEnumerable()
        {
            //Prepare the base dictionary
            Dictionary<string, Permission> permissions = new Dictionary<string, Permission>(base.permissions.Count + 1 + 10);

            //Add everyone
            var everyone = Permission.FromGroup("everyone", StateType.Allow);
            permissions[everyone.Name] = everyone;

            //Add the roles
            foreach(var role in Member.Roles)
            {
                var perm = Permission.FromGroup($"role.{role.Id}", StateType.Unset);
                permissions[perm.Name] = perm;
            }

            //Return the dictionary
            return permissions.Values;
        }

        public override Dictionary<string, string> ToDictionary()
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            foreach (var p in base.PermissionsEnumerable()) dictionary.Add(p.Name, p.State.ToString());
            return dictionary;
        }

        public static string GetGroupName(DiscordMember member) => $"user.{member.Id}";
    }
}
