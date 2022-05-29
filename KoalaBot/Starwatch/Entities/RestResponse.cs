using KoalaBot.Starwatch.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Starwatch.Entities
{
    /// <summary>
    /// Raw response without a payload from Starwatch
    /// </summary>
    public class RestResponse
    {
        /// <summary>
        /// Status of the HTTP request
        /// </summary>
        [JsonProperty("Status")]
        public RestStatus Status { get; protected set; }

        /// <summary>
        /// Was the request succesful?
        /// </summary>
        public bool Success => Status == RestStatus.OK || Status == RestStatus.Async;
        public bool IsAsync => Status == RestStatus.Async;

        /// <summary>
        /// The route that was taken
        /// </summary>
        [JsonProperty("Route", NullValueHandling = NullValueHandling.Ignore)]
        public string Route { get; protected set; } = null;

        /// <summary>
        /// The error message, if any.
        /// </summary>
        [JsonProperty("Message")]
        public string Message { get; protected set; }

        /// <summary>
        /// The type Starwatch serialized
        /// </summary>
        [JsonProperty("Type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; protected set; }

        //[JsonProperty("Response")]
        //protected JObject Response { get; set; }

        /// <summary>
        /// The time in milliseconds it took to execute the endpoint server side.
        /// </summary>
        [JsonProperty("ExecuteTime", NullValueHandling = NullValueHandling.Ignore)]
        public double ExecuteTime { get; protected set; }

        /// <summary>
        /// Validates the request was succesfull. Throws exception otherwise
        /// </summary>
        public void Validate()
        {
            if (!Success)
                throw new RestResponseException(this);
        }
    }


    /// <summary>
    /// Response object from Starwatch
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Response<T> : RestResponse
    {
        /// <summary>
        /// The object that is being fetched.
        /// </summary>
        [JsonProperty("Response")]
        public T Payload { get; private set; }

        public Response()
        {
            Payload = default(T);
        }

        internal Response(Response<JToken> response) : this()
        {
            Status = response.Status;
            Route = response.Route;
            Message = response.Message;
            Type = response.Type;
            ExecuteTime = response.ExecuteTime;

            if (response.Payload != null && response.Status != RestStatus.ResourceNotFound)
                Payload = response.Payload.ToObject<T>();
        }
    }
}
