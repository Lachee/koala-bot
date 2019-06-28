using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Permissions.CommandNext
{
    public class PermissionGroupConverter : IArgumentConverter<Group>
    {
        public async Task<Optional<Group>> ConvertAsync(string value, CommandContext ctx)
        {
            //Try to get the group manager
            GuildManager manager = Koala.Bot?.PermissionManager?.GetGuildManager(ctx.Guild);
            if (manager == null)
                return Optional.FromNoValue<Group>();

            //Try to get the group
            Group group = await manager.GetGroupAsync(value.Equals("default") ? GuildManager.DEFAULT_GROUP : value);
            if (group != null) return Optional.FromValue(group);
            
            //We failed to get member, so lets try role  
            try
            {
                var role = value.Equals("everyone") ? ctx.Guild.EveryoneRole : await ctx.CommandsNext.ConvertArgument<DiscordRole>(value, ctx) as DiscordRole;
                if (role != null)
                {
                    group = await manager.GetRoleGroupAsync(role);
                    return Optional.FromValue(group);
                }
            }
            catch
            {
            }

            //We failed to get a group, so try other things
            try
            {
                var member = await ctx.CommandsNext.ConvertArgument<DiscordMember>(value, ctx) as DiscordMember;
                if (member != null)
                {
                    group = await manager.GetMemberGroupAsync(member);
                    return Optional.FromValue(group);
                }
            }
            catch
            {
            }
            
            //We failed to get anything, so return no value
            return Optional.FromNoValue<Group>();
        }
    }

    public class PermissionMemberGroupConverter : IArgumentConverter<MemberGroup>
    {
        public async Task<Optional<MemberGroup>> ConvertAsync(string value, CommandContext ctx)
        {
            //Try to get the group manager
            GuildManager manager = Koala.Bot?.PermissionManager?.GetGuildManager(ctx.Guild);
            if (manager == null) return Optional.FromNoValue<MemberGroup>();

            //We failed to get a group, so try other things
            var member = await ctx.CommandsNext.ConvertArgument<DiscordMember>(value, ctx) as DiscordMember;
            if (member != null)
            {
                var group = await manager.GetMemberGroupAsync(member);
                return Optional.FromValue(group);
            }
            
            //Return nothing
            return Optional.FromNoValue<MemberGroup>();
        }
    }
}
