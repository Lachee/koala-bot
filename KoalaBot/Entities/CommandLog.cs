using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using KoalaBot.Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Entities
{
    public class CommandLog : IRecord
    {
        public long Id { get; private set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public ulong UserId { get; set; }
        public string Content { get; set; }
        public string Name { get; set; }
        public int AttachmentCount { get; set; }
        public string Failure { get; set; }
        public DateTime DateCreated { get; private set; }

        public CommandLog()
        {
            Id = 0;
            GuildId = 0;
            ChannelId = 0;
            MessageId = 0;
            UserId = 0;
            Failure = null;
        }

        public CommandLog(CommandContext ctx, string failure = null) : this()
        {
            GuildId = ctx.Guild.Id;
            ChannelId = ctx.Channel.Id;
            MessageId = ctx.Message.Id;
            UserId = ctx.Member.Id;
            Content = ctx.Message.Content;
            Name = ctx.Command?.QualifiedName;
            AttachmentCount = ctx.Message.Attachments.Count;
            Failure = failure;
        }

        /// <summary>
        /// Gets the probably reply to this command.
        /// </summary>
        /// <param name="koala"></param>
        /// <returns></returns>
        public Task<Reply> GetReplyAsync(Koala koala = null)
        {
            koala = koala ?? Koala.Bot;
            return koala.ReplyManager.GetReplyAsync(GuildId, MessageId);
        }

        public async Task<bool> LoadAsync(DbContext db)
        {
            return null != await db.SelectOneAsync("!cmdlog", (reader) =>
            {
                GuildId         = reader.GetUInt64("guild");
                ChannelId       = reader.GetUInt64("channel");
                MessageId       = reader.GetUInt64("message");
                UserId          = reader.GetUInt64("user");

                Content         = reader.GetString("content");
                Name            = reader.GetString("name");
                AttachmentCount = reader.GetInt32("attachments");
                Failure         = reader.GetString("failure");
                DateCreated     = reader.GetDateTime("date_created");
                return this;
            }, new Dictionary<string, object>() { { "id", Id } });
        }

        public async Task<bool> SaveAsync(DbContext db)
        {
            Dictionary<string, object> columns = new Dictionary<string, object>()
            {
                ["guild"] = GuildId,
                ["channel"] = ChannelId,
                ["message"] = MessageId,
                ["user"] = UserId,
                ["content"] = Content,
                ["name"] = Name,
                ["attachments"] = AttachmentCount,
                ["failure"] = string.IsNullOrWhiteSpace(Failure) ? null : Failure
            };

            if (Id > 0) columns.Add("id", Id);
            
            long insertedId = await db.InsertUpdateAsync("!cmdlog", columns);
            if (insertedId > 0) Id = insertedId;
            return insertedId > 0;
        }
    }
}
