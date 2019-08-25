using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using KoalaBot.Extensions;
using KoalaBot.PermissionEngine;
using KoalaBot.PermissionEngine.Groups;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.CommandNext
{
    public class PermissionArgumentConverter : IArgumentConverter<Group>
    {
        public async Task<Optional<Group>> ConvertAsync(string value, CommandContext ctx)
        {
            //Try to get the group manager
            Engine engine = Koala.Bot?.PermissionManager?.GetEngine(ctx.Guild);
            if (engine == null) return Optional.FromNoValue<Group>();

            //Try to get the group
            Group group = await engine.GetGroupAsync(value);
            if (group != null) return Optional.FromValue(group);

            try
            {
                //Try to get a role based group
                var role = value.Equals("everyone") ? ctx.Guild.EveryoneRole : await ctx.CommandsNext.ConvertArgument<DiscordRole>(value, ctx) as DiscordRole;
                if (role != null)
                {
                    group = await engine.GetGroupAsync(role.GetGroupName());
                    return Optional.FromValue(group);
                }
            } catch { }

            //We failed to get anything, so return no value
            var result = await MemberPermissionArgumentConverter.ConvertAsync(ctx, engine, value);
            if (result.HasValue) return Optional.FromValue<Group>((Group)result);
            return Optional.FromNoValue<Group>();
        }

    }

    public class MemberPermissionArgumentConverter : IArgumentConverter<MemberGroup>
    {
        public async Task<Optional<MemberGroup>> ConvertAsync(string value, CommandContext ctx)
        { 
            //Try to get the group manager
            Engine engine = Koala.Bot?.PermissionManager?.GetEngine(ctx.Guild);
            if (engine == null) return Optional.FromNoValue<MemberGroup>();

            return await ConvertAsync(ctx, engine, value);
        }


        public static async Task<Optional<MemberGroup>> ConvertAsync(CommandContext ctx, Engine engine, string value)
        {
            //Try to get a member based role instead
            var member = await ctx.CommandsNext.ConvertArgument<DiscordMember>(value, ctx) as DiscordMember;
            if (member != null)
            {
                var manager = Koala.Bot?.PermissionManager;
                if (manager == null) return Optional.FromNoValue<MemberGroup>();

                try
                {
                    var group = await manager.GetMemberGroupAsync(member);
                    return Optional.FromValue(group);
                }
                catch (Exception e)
                {
                    Koala.Bot.Logger.LogError(e);
                }
            }

            return Optional.FromNoValue<MemberGroup>();
        }
    }
}
