using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Starwatch.Entities
{
    public class RateLimit
    {
        public DateTime? RetryAfter { get; set; }
        public int Limit { get; set; }
        public int Remaining { get; set; }
    }
}
