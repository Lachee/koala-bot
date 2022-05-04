using KoalaBot.Starwatch.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Starwatch.Responses
{
    public class PlayerKickResponse
    {
        public List<Player> Players { get; set; } = new List<Player>();
        public int SuccessfulKicks { get; set; } = 0;
        public int FailedKicks { get; set; } = 0;
    }
}
