using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using KoalaBot.Redis;
using KoalaBot.Extensions;
using KoalaBot.Logging;
using DSharpPlus.Entities;
using System.Linq;
using KoalaBot.CommandNext;

namespace KoalaBot.Modules
{
    [Group("tag")]
    [Permission("koala.tag")]
    public class TagModule : BaseCommandModule
    {
        public Koala Bot { get; }
        public IRedisClient Redis => Bot.Redis;
        public Logger Logger { get; }

        public TagModule(Koala bot)
        {
            this.Bot = bot;
            this.Logger = new Logger("CMD-CONFIG", bot.Logger);
        }

        [Command("remove")]
        [Aliases("r", "delete", "d", "trash", "t")]
        [Description("Removes a tag")]
        [Permission("koala.tag.delete")]
        public async Task ExecuteRemoveTag(CommandContext ctx, [RemainingText, Description("The tag name to remove")] string name)
        {
            name = name.ToLowerInvariant();     //First thing is we will lower the name. All names will be case insensitive.

            //Make sure the redis command doesnt exist
            Tag tag = await GetTagAsync(ctx.Guild, name);
            if (tag == null) throw new ArgumentException($"The tag {name} does not exist", nameof(name));

            //Make sure we are the owner
            if (ctx.Member.Id != tag.Owner && !await ctx.Member.HasPermissionAsync("koala.tag.delete.others"))
                throw new Exception("You cannot remove someone else's tag.");

            await RemoveTagAsync(ctx.Guild, tag);
            await ctx.ReplyReactionAsync(true);
        }

        [Command("create")]
        [Aliases("c")]
        [Description("Creates a new tag")]
        [Permission("koala.tag.create")]
        public async Task ExecuteCreateTag(CommandContext ctx, string name, [RemainingText] string content)
        {
            name = name.ToLowerInvariant();     //First thing is we will lower the name. All names will be case insensitive.

            //Make sure it doesnt contain illegal characters
            if (name.Contains(":"))
                throw new ArgumentException($"Name of the tag cannot contain :", nameof(name));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace.", nameof(name));

            //Make sure we dont create a tag for a command
            var cmd = Bot.CommandsNext.FindCommand($"tag {name}", out _);
            if (cmd != null && !cmd.Name.Equals("tag", StringComparison.InvariantCultureIgnoreCase)) 
                throw new ArgumentException($"Name of the tag cannot be {name} as it reserved for the tag {cmd.Name} command.", nameof(name));

            //Make sure the redis command doesnt exist
            Tag tag = await GetTagAsync(ctx.Guild, name);
            if (tag != null)
                throw new ArgumentException($"Name of the tag cannot be {name} as it already exists.", nameof(name));

            //Create the tag
            await CreateTagAsync(ctx.Guild, ctx.Member, name, content);

            //Respond with the new tag
            await ctx.RespondAsync($"**Tag {name} Preview:**");
            await ExecuteGroupCommand(ctx, name);
        }

        [GroupCommand]
        [Description("Recalls and displays a tag")]
        [Permission("koala.tag.show")]
        public async Task ExecuteGroupCommand(CommandContext ctx, [RemainingText, Description("The name of the tag to display.")] string name)
        {
            name = name.ToLowerInvariant();     //First thing is we will lower the name. All names will be case insensitive.
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace.", nameof(name));
            
            //Make sure the redis command doesnt exist
            Tag tag = await GetTagAsync(ctx.Guild, name);
            if (tag == null)
            {
                await ctx.ReplyReactionAsync(false);
                return;
            }

            //Respond with the tag
            await ctx.RespondAsync(tag.Content);
        }

        /// <summary>Fetches a tag</summary>
        public async Task<Tag> GetTagAsync(DiscordGuild guild, string name)
        {
            name = name.ToLowerInvariant();     //First thing is we will lower the name. All names will be case insensitive.
            var tag = await Redis.FetchObjectAsync<Tag>(Namespace.Combine(guild.Id, "tags", name));
            if (tag != null) tag.Name = name;
            return tag;
        }

        /// <summary>Edits the tag</summary>
        public async Task EditTagAsync(DiscordGuild guild, Tag tag, string content) => await Redis.StoreStringAsync(Namespace.Combine(guild.Id, "tags", tag.Name), Tag.KEY_CONTENT, content);

        /// <summary>Creates a tag</summary>
        public async Task<Tag> CreateTagAsync(DiscordGuild guild, DiscordUser owner, string name, string content)
        {
            Tag tag = new Tag()
            {
                Creator = owner.Id,
                Owner = owner.Id,
                Content = content,
                DateCreated = DateTime.UtcNow
            };
            
            var transaction = Redis.CreateTransaction();
            _ = transaction.StoreObjectAsync(Namespace.Combine(guild.Id, "tags", name), tag);       //Store tag
            _ = transaction.AddHashSetAsync(Namespace.Combine(guild.Id, "tags"), name);             //Add to guild list
            _ = transaction.AddHashSetAsync(Namespace.Combine(guild.Id, owner.Id, "tags"), name);   //Add to owner list
            await transaction.ExecuteAsync();
            return tag;
        }

        /// <summary>Transfers a tag a tag</summary>
        public async Task TransferTagAsync(DiscordGuild guild, DiscordUser newOwner, Tag tag)
        {
            var transaction = Redis.CreateTransaction();
            _ = transaction.AddHashSetAsync(Namespace.Combine(guild.Id, newOwner.Id, "tags"), tag.Name);                                //Add to new user list
            _ = transaction.RemoveHashSetAsync(Namespace.Combine(guild.Id, tag.Owner, "tags"), tag.Name);                               //Remove from old user list
            _ = transaction.StoreStringAsync(Namespace.Combine(guild.Id, "tags", tag.Name), Tag.KEY_OWNER, newOwner.Id.ToString());     //Edit owner
            await transaction.ExecuteAsync();
        }

        /// <summary>Removes a tag</summary>
        public async Task RemoveTagAsync(DiscordGuild guild, Tag tag)
        {
            var transaction = Redis.CreateTransaction();
            _ = transaction.RemoveHashSetAsync(Namespace.Combine(guild.Id, "tags"), tag.Name);              //Remove from guild list
            _ = transaction.RemoveAsync(Namespace.Combine(guild.Id, "tags", tag.Name));                     //Remove from owner list
            _ = transaction.RemoveHashSetAsync(Namespace.Combine(guild.Id, tag.Owner, "tags"), tag.Name);   //Remove tag
            await transaction.ExecuteAsync();
        }


    }

    public class Tag
    {
        public const string KEY_CONTENT = "Content";
        public const string KEY_CREATOR = "Creator";
        public const string KEY_OWNER = "Owner";

        [Redis.Serialize.RedisIgnore]
        public string Name { get; set; }

        [Redis.Serialize.RedisProperty(KEY_CONTENT)]
        public string Content { get; set; }

        [Redis.Serialize.RedisProperty(KEY_CREATOR)]
        public ulong Creator { get; set; }

        [Redis.Serialize.RedisProperty(KEY_OWNER)]
        public ulong Owner { get; set; }

        [Redis.Serialize.RedisIgnore]
        public DateTime DateCreated { get; set; }

        [Redis.Serialize.RedisProperty("DateCreated")]
        public double OADateCreated
        {
            get { return DateCreated.ToOADate(); }
            set { DateCreated = DateTime.FromOADate(value); }
        }
    }
}
