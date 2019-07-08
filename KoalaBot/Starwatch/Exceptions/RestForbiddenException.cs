using KoalaBot.Starwatch.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace KoalaBot.Starwatch.Exceptions
{
    public class RestForbiddenException : RestResponseException
    {
        public RestForbiddenException(RestResponse response) : base(response)
        {
            Debug.Assert(response.Status == RestStatus.Forbidden, "Forbidden Exception thrown on non-forbidden status.");
        }
    }
}
