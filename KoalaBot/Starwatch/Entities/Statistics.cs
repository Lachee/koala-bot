using KoalaBot.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Starwatch.Entities
{
    public class Statistics
    {
        public int Connections { get; set; }
        public int? LastConnectionID { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double Uptime { get; set; }
        public int TotalAccounts { get; set; }
        public string LastShutdownReason { get; set; }
        public bool IsRunning => StartTime > EndTime;

        [JsonProperty("MemoryUsage")]
        public MemoryUsage Memory { get; set; }
        public struct MemoryUsage
        {
            public long WorkingSet { get; set; }
            public long PeakWorkingSet { get; set; }
            public long MaxWorkingSet { get; set; }
        }

        /// <summary>
        /// Formats the uptime, returning true if the server is up, otherwise false.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public bool FormatUptime(out string format)
        {
            if (IsRunning)
            {
                format = (DateTime.UtcNow - StartTime).Format();
                return true;
            }
            else
            {
                format = (DateTime.UtcNow - EndTime).Format() + " Ago";
                return false;
            }
        }
    }
}
