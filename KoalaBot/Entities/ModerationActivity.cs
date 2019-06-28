using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Entities
{
    public class ModerationActivity
    {

        public ulong ModeratorId { get; set; }
        public string Reason { get; set; }
        public ModerationActivity() { }
        public ModerationActivity(DiscordMember moderator, string reason)
        {
            this.Reason = reason;
            this.ModeratorId = moderator.Id;
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
