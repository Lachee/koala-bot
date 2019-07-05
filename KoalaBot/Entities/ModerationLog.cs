using DSharpPlus.Entities;
using KoalaBot.Database;
using KoalaBot.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Entities
{
    public class ModerationLog : IData
    {
        public ulong Id { get; private set; }
        public ulong GuildId { get; set; }
        public string Action { get; set; }
        public string Reason { get; set; }
        public ulong SubjectId { get; set; }
        public ulong ModeratorId { get; set; }
        public ulong Message { get; set; }
        public DateTime DateTime { get; private set; }

        public ModerationLog()
        {
            Id = 0;
            GuildId = 0;
            Action = "";
            Reason = "";
            SubjectId = 0;
            ModeratorId = 0;
            Message = 0;
            DateTime = DateTime.Now;
        }

        public ModerationLog(string action, DiscordGuild guild, DiscordUser subject, DiscordUser moderator = null, string reason = null) : this()
        {
            Action = action;
            Reason = reason;
            GuildId = guild.Id;
            ModeratorId = (moderator?.Id).GetValueOrDefault();
            SubjectId = subject.Id;
        }

        public ModerationLog(string action, DiscordMember moderator, DiscordUser subject, string reason) : this()
        {
            Action = action;
            Reason = reason;
            GuildId = moderator.Guild.Id;
            ModeratorId = moderator.Id;
            SubjectId = subject.Id;
        }

        public async Task LoadAsync(DbContext db)
        {
            await db.SelectOneAsync("!modlog", (reader) =>
            {
                Id = (ulong)reader["id"];
                Action = (string)reader["action"];
                GuildId = (ulong)reader["guild"];
                Reason = (string)reader["reason"];
                SubjectId = (ulong)reader["subject"];
                ModeratorId = (ulong)reader["moderator"];
                DateTime = (DateTime)reader["date"];
                Message = (ulong)reader["message"];

                return Task.CompletedTask;
            }, new Dictionary<string, object>() { { "id", Id } });
        }

        public async Task SaveAsync(DbContext db)
        {
            var content = new Dictionary<string, object>()
            {
                { "action", Action },
                { "reason", Reason },
                { "subject", SubjectId },
                { "moderator", ModeratorId },
                { "guild", GuildId },
                { "message", Message },
            };

            //Add the ID if we have one
            if (Id > 0) content.Add("id", Id);

            //Insert the database and update our ID (if we actually inserted)
            var insertedId = await db.InsertUpdateAsync("!modlog", content);
            if (insertedId > 0) Id = (ulong)insertedId;
        }
    }
}
