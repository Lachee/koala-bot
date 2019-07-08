using KoalaBot.Starwatch.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Starwatch.Exceptions
{
    public class RestResponseException : Exception
    {
        public string Route { get; }
        public RestStatus Status { get; }
        public string RestMessage { get; }

        public RestResponseException(RestResponse response) : base($"Rest Failed ({response.Status.ToString()}) on route {response.Route}: {response.Message}")
        {
            Route = response.Route;
            Status = response.Status;
            RestMessage = response.Message;
        }
    }
}
