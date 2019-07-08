using KoalaBot.Starwatch.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace KoalaBot.Starwatch.Exceptions
{
    public class RestRouteNotFoundException : RestResponseException
    {
        public string Endpoint { get; }
        public RestRouteNotFoundException(RestResponse response) : this(response, null) { } 
        public RestRouteNotFoundException(RestResponse response, string endpoint) : base(response)
        {
            Endpoint = endpoint ?? response.Route;
            Debug.Assert(response.Status == RestStatus.RouteNotFound, "404 Exception thrown on non-404 status.");
        }
    }
}
