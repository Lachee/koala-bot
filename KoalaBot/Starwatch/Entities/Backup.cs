using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Starwatch.Entities
{
    public class Backup
    {
        public bool IsRolling { get; set; }
        public bool IsAutoRestore { get; set; }
        public DateTime? LastBackup { get; set; }
    }

}
