using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Starwatch.Entities
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Account
    {
        public string Name { get; set; } = null;
        public bool? IsAdmin { get; set; } = null;
        public bool? IsActive { get; set; } = null;
        public string Password { get; set; } = null;

    }
}
