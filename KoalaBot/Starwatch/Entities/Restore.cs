using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Starwatch.Entities
{
    public class Restore
    {
        public string World { get; set; }
        public string Mirror { get; set; }
        public int Priority { get; set; }
    }

}
