using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Starwatch.Entities
{
    public class ProtectedAccount
    {
        public long ProtectionId { get; set; }
        public string AccountName { get; set; }
        public string Reason { get; set; }
    }
}
