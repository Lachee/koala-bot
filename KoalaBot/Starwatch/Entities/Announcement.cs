using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Starwatch.Entities
{
    public class Announcement
    {
        public string Message { get; set; }
        public bool Enabled { get; set; }
        public double Interval { get; set; }
    }
}
