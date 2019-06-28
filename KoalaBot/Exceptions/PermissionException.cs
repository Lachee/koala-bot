using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Exceptions
{
    class PermissionException : Exception
    {
        public string Permission { get; set; }

        public PermissionException(string permission) : base("Invalid Permission.")
        {
            Permission = permission;
        }

        public PermissionException(string message, string permission) : base(message)
        {
            Permission = permission;
        }
    }
}
