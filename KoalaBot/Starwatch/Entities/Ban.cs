using KoalaBot.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Starwatch.Entities
{
    public class Ban
    {
        [JsonProperty("ticket", NullValueHandling = NullValueHandling.Ignore)]
        public long? Ticket { get; internal set; }

        [JsonProperty("ip", NullValueHandling = NullValueHandling.Ignore)]
        public string IP { get; set; }

        [JsonProperty("uuid", NullValueHandling = NullValueHandling.Ignore)]
        public string UUID { get; set; }

        [JsonProperty("reason", NullValueHandling = NullValueHandling.Ignore)]
        public string Reason { get; set; }

        [JsonProperty("bannedBy", NullValueHandling = NullValueHandling.Ignore)]
        public string Moderator { get; set; }

        [JsonProperty("bannedAt", NullValueHandling = NullValueHandling.Ignore)]
        private long _bannedAt
        {
            get => CreatedDate.HasValue ? CreatedDate.Value.ToUnixEpoch() : 0;
            set => CreatedDate = value.ToDateTime();
        }

        /// <summary>
        /// The time the ban started
        /// </summary>
        [JsonIgnore]
        public DateTime? CreatedDate { get; internal set; }

    }
}
