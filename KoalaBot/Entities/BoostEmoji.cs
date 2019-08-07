using DSharpPlus.Entities;
using KoalaBot.Database;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Entities
{
    public class BoostEmoji : IRecord
    {
        public long Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong OwnerId { get; set; }
        public ulong EmojiId { get; set; }
        public string Name { get; set; }
        public string URL { get; set; }

        public BoostEmoji() { }
        public BoostEmoji(DiscordMember member)
        {
            GuildId = member.Guild.Id;
            OwnerId = member.Id;
        }

        public async Task<bool> LoadAsync(DbContext db)
        {
            BoostEmoji emoji;
            if (Id >= 0)
                emoji = await db.SelectOneAsync<BoostEmoji>("!emojis", ReadEmoji, new Dictionary<string, object>() { { "id", Id } });
            else
                emoji = await db.SelectOneAsync<BoostEmoji>("!emojis", ReadEmoji, new Dictionary<string, object>() { { "guild", GuildId }, { "owner", OwnerId } });

            if (emoji != null && emoji.Id > 0)
            {
                Id = emoji.Id;
                GuildId = emoji.GuildId;
                OwnerId = emoji.OwnerId;
                EmojiId = emoji.EmojiId;
                Name = emoji.Name;
                URL = emoji.URL;
                return true;
            }

            return false;
        }

        public async Task<bool> DeleteAsync(DbContext db)
        {
            return await db.DeleteAsync("!emojis", new Dictionary<string, object>() { ["id"] = Id });
        }

        private BoostEmoji ReadEmoji(DbDataReader reader)
        {
            BoostEmoji emoji = new BoostEmoji()
            {
                Id = reader.GetInt64("id"),
                GuildId = reader.GetUInt64("guild"),
                OwnerId = reader.GetUInt64("owner"),
                EmojiId = reader.GetUInt64("emoji"),
                Name = reader.GetString("name"),
                URL = reader.GetString("url")
            };

            return emoji;
        }

        public async Task<bool> SaveAsync(DbContext db)
        {
            var columns = new Dictionary<string, object>()
            {
                ["guild"] = GuildId,
                ["owner"] = OwnerId,
                ["emoji"] = EmojiId,
                ["name"] = Name,
                ["url"] = URL,
            };

            if (Id > 0) columns.Add("id", Id);
            return await db.InsertUpdateAsync("!emojis", columns) > 0;
        }
    }
}
