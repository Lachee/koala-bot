using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Entities
{
    public class ModerationActivity
    {
        public const string KEY_MODERATOR_ID = "modid";
        public const string KEY_MODERATOR_NAME = "modname";
        public const string KEY_REASON = "reason";

        [Redis.Serialize.RedisProperty(KEY_MODERATOR_ID)]
        public ulong ModeratorId { get; set; }

        [Redis.Serialize.RedisProperty(KEY_MODERATOR_NAME)]
        public string ModeratorName { get; set; }

        [Redis.Serialize.RedisProperty(KEY_REASON)]
        public string Reason { get; set; }

        public ModerationActivity() { }
        public ModerationActivity(DiscordMember moderator, string reason)
        {
            this.Reason = reason;
            this.ModeratorId = moderator.Id;
            this.ModeratorName = moderator.Username;
        }
    }

    public class EnforceNicknameActivity : ModerationActivity
    {
        public const string KEY_NICKNAME = "nickname";

        [Redis.Serialize.RedisProperty(KEY_NICKNAME)]
        public string Nickname { get; set; }

        public EnforceNicknameActivity() : base() { }
        public EnforceNicknameActivity(string nickname, DiscordMember moderator, string reason) : base(moderator, reason)
        {
            Nickname = nickname;
        }
    }
}
