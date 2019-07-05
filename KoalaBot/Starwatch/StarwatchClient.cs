using KoalaBot.Starwatch.Entities;
using KoalaBot.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Starwatch
{
    public class StarwatchClient
    {
        public string Host { get; }

        private HttpClient _client;

        public StarwatchClient(string host, string username, string password)
        {
            Host = host.TrimEnd('/');
            _client = new HttpClient();

            //Set the auth header
            var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }

        /// <summary>
        /// Searches for an account
        /// </summary>
        /// <param name="account"></param>
        /// <param name="character"></param>
        /// <param name="ip"></param>
        /// <param name="uuid"></param>
        /// <returns></returns>
        public async Task<Response<Session[]>> GetSessionsAsync(string account = null, string character = null, string ip = null, string uuid = null)
        {
            var queries = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(account))     queries.Add("account", account);
            if (!string.IsNullOrEmpty(character))   queries.Add("character", character);
            if (!string.IsNullOrEmpty(ip))          queries.Add("ip", ip);
            if (!string.IsNullOrEmpty(uuid))        queries.Add("uuid", uuid);
            return await GetRequestAsync<Session[]>("/session", queries);
        }




        /// <summary>
        /// Sends a GET request to a specified endpoint.
        /// </summary>
        public async Task<Response<object>> GetRequestAsync(string endpoint)
        {
            return await GetRequestAsync<object>(endpoint);
        }

        /// <summary>
        /// Sends a GET request to a specified endpoint.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        private async Task<Response<T>> GetRequestAsync<T>(string endpoint, IEnumerable<KeyValuePair<string, object>> queries = null)
        {
            var url = BuildUrl(endpoint, queries);
            string json = await _client.GetStringAsync(url);
            return JsonConvert.DeserializeObject<Response<T>>(json);
        }

        private Uri BuildUrl(string endpoint, IEnumerable<KeyValuePair<string, object>> queries)
        {

            string url = $"{Host}/api{endpoint.TrimEnd('/')}";
            if (queries != null)
            {
                var q = string.Join("&", queries.Select(kp => $"{kp.Key}={kp.Value}"));
                if (!string.IsNullOrEmpty(q)) url += $"?{q}";
            }

            return new Uri(url);
        }
    }
}
