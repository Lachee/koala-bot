using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Exceptions
{
    class PermissionException : Exception
    {
        public PermissionException(string message) : base(message) { }
    }
}
