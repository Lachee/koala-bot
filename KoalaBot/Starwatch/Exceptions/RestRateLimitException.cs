using KoalaBot.Starwatch.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace KoalaBot.Starwatch.Exceptions
{
    public class RestRateLimitException : RestResponseException
    {
        public RateLimit RateLimit { get; }

        public RestRateLimitException(Response<RateLimit> response) : base(response)
        {
            RateLimit = response.Payload;
        }
    }
}
