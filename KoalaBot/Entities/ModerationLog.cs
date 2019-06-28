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
        public ulong Guild { get; set; }
        public string Action { get; set; }
        public string Reason { get; set; }
        public ulong Subject { get; set; }
        public ulong Moderator { get; set; }
        public ulong Message { get; set; }
        public DateTime DateTime { get; private set; }

        public ModerationLog()
        {
            Id = 0;
            Guild = 0;
            Action = "";
            Reason = "";
            Subject = 0;
            Moderator = 0;
            Message = 0;
            DateTime = DateTime.Now;
        }

        public ModerationLog(string action, DiscordMember moderator, DiscordUser subject, string reason) : this()
        {
            Action = action;
            Reason = reason;
            Guild = moderator.Guild.Id;
            Moderator = moderator.Id;
            Subject = subject.Id;
        }

        public async Task LoadAsync(DatabaseClient db)
        {
            await db.ExecuteOneAsync("SELECT * FROM !modlog WHERE id=?id", (reader) =>
            {
                Id = (ulong)reader["id"];
                Action = (string)reader["action"];
                Guild = (ulong)reader["guild"];
                Reason = (string)reader["reason"];
                Subject = (ulong)reader["subject"];
                Moderator = (ulong)reader["moderator"];
                DateTime = (DateTime)reader["date"];
                Message = (ulong)reader["message"];

                return Task.CompletedTask;
            }, new Dictionary<string, object>() { { "id", Id } });
        }

        public async Task SaveAsync(DatabaseClient db)
        {
            var content = new Dictionary<string, object>()
            {
                { "action", Action },
                { "reason", Reason },
                { "subject", Subject },
                { "moderator", Moderator },
                { "guild", Guild },
                { "message", Message },
            };

            //Add the ID if we have one
            if (Id > 0) content.Add("id", Id);

            //Insert the database and update our ID (if we actually inserted)
            var insertedId = await db.InsertOrUpdateAsync("!modlog", content);
            if (insertedId > 0) Id = (ulong)insertedId;
        }
    }
}
