using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Starwatch.Entities
{
    public class Response<T>
    {
        [JsonProperty("Status")]
        public RestStatus Status { get; private set; }

        [JsonProperty("Route", NullValueHandling = NullValueHandling.Ignore)]
        public string Route { get; private set; } = null;

        [JsonProperty("Message")]
        public string Message { get; }

        [JsonProperty("Type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; private set; }

        [JsonProperty("Response")]
        public T Object { get; private set; }

        [JsonProperty("ExecuteTime", NullValueHandling = NullValueHandling.Ignore)]
        public double Time { get; private set; }
    }
}
