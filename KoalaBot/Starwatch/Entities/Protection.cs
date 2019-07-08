using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Starwatch.Entities
{
    public class Protection
    {
        public string Name { get; set; }
        public string Whereami
        {
            get => World?.Whereami;
            set => World = World.Parse(value);
        }

        [JsonIgnore]
        public World World { get; set; }
        public bool AllowAnonymous { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ProtectionMode Mode { get; set; }
        public enum ProtectionMode
        {
            Blacklist,
            Whitelist
        }
        
    }
}
