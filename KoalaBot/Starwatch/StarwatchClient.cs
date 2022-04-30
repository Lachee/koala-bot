using KoalaBot.Starwatch.Entities;
using KoalaBot.Starwatch.Exceptions;
using KoalaBot.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace KoalaBot.Starwatch
{
    public class StarwatchClient
    {
        public string Host { get; }

        private HttpClient _httpClient;
        private string _authorization;

        public StarwatchClient(string host, string username, string password)
        {
            Host = host.TrimEnd('/');
            _httpClient = new HttpClient();

            //Set the auth header
            var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            _authorization = Convert.ToBase64String(byteArray);

            //_client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
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
            if (!string.IsNullOrEmpty(account)) queries.Add("account", account);
            if (!string.IsNullOrEmpty(character)) queries.Add("username", character);
            if (!string.IsNullOrEmpty(ip)) queries.Add("ip", ip);
            if (!string.IsNullOrEmpty(uuid)) queries.Add("uuid", uuid);
            return await GetRequestAsync<Session[]>("/session", queries);
        }

        /// <summary>
        /// Reloads the server settings
        /// </summary>
        /// <param name="wait"></param>
        /// <returns></returns>
        public async Task<RestResponse> ReloadAsync(bool wait)
        {
            return await PutRequestAsync<object>("/server", new Dictionary<string, object>() { ["async"] = !wait });
        }

        /// <summary>
        /// Restarts the server
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="wait"></param>
        /// <returns></returns>
        public async Task<RestResponse> RestartAsync(string reason, bool wait)
        {
            return await DeleteRequestAsync<object>("/server", new Dictionary<string, object>() { ["reason"] = reason, ["async"] = !wait });
        }

        #region protection

        /// <summary>
        /// Gets a protection for the world
        /// </summary>
        /// <param name="world"></param>
        /// <returns></returns>
        public async Task<Response<Protection>> GetProtectionAsync(World world) => await GetRequestAsync<Protection>($"/world/{world.Whereami}/protection");


        /// <summary>
        /// Creates a new world protection
        /// </summary>
        /// <param name="protection"></param>
        /// <returns></returns>
        public async Task<Response<Protection>> CreateProtectionAsync(Protection protection) => await PostRequestAsync<Protection>($"/world/{protection.Whereami}/protection", payload: protection);

        /// <summary>
        /// Deletes a world protection
        /// </summary>
        /// <param name="world"></param>
        /// <returns></returns>
        public async Task<RestResponse> DeleteProtectionAsync(World world) => await DeleteRequestAsync<object>($"/world{world.Whereami}/protection");

        /// <summary>
        /// Gets the protected account
        /// </summary>
        /// <param name="world"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        public async Task<Response<ProtectedAccount>> GetProtectionAccountAsync(World world, string account) => await GetRequestAsync<ProtectedAccount>($"/world/{world.Whereami}/protection/{account}");

        /// <summary>
        /// Adds an account to the list
        /// </summary>
        /// <param name="world"></param>
        /// <param name="account"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task<Response<object>> CreateProtectedAccountAsync(World world, string account, string reason) => await PostRequestAsync<object>($"/world/{world.Whereami}/protection/{account}", new Dictionary<string, object>() { ["reason"] = reason });

        /// <summary>
        /// Deletes an account from the list
        /// </summary>
        /// <param name="world"></param>
        /// <param name="account"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task<Response<bool>> DeleteProtectedAccountAsync(World world, string account, string reason) => await DeleteRequestAsync<bool>($"/world/{world.Whereami}/protection/{account}", new Dictionary<string, object>() { ["reason"] = reason });

        #endregion

        #region Backup

        /// <summary>
        /// Gets the restore information
        /// </summary>
        /// <param name="world"></param>
        /// <returns></returns>
        public async Task<Response<Restore>> GetRestoreAsync(World world) => await GetRequestAsync<Restore>($"/world/{world.Whereami}/restore");

        /// <summary>
        /// Sets the mirror information
        /// </summary>
        /// <param name="world"></param>
        /// <param name="backup"></param>
        /// <returns></returns>
        public async Task<Response<Restore>> SetRestoreMirrorAsync(World world, World mirror) => await PutRequestAsync<Restore>($"/world/{world.Whereami}/restore", payload: new Restore() { World = world.Whereami, Mirror = mirror.Whereami });

        /// <summary>
        /// Deletes the restore
        /// </summary>
        /// <param name="world"></param>
        /// <param name="backup"></param>
        /// <returns></returns>
        public async Task<Response<bool>> DeleteRestoreAsync(World world) => await DeleteRequestAsync<bool>($"/world/{world.Whereami}/restore");

        /// <summary>
        /// Creates a new restore.
        /// </summary>
        /// <param name="world"></param>
        /// <returns></returns>
        public async Task<Response<Restore>> CreateRestoreAsync(World world) => await PostRequestAsync<Restore>($"/world/{world.Whereami}/restore", payload: new Restore() { World = world.Whereami });

        #endregion

        #region Ban

        /// <summary>
        /// Creates a new ban
        /// </summary>
        /// <param name="ban"></param>
        /// <returns></returns>
        public async Task<Response<Ban>> BanAsync(Ban ban) => await PostRequestAsync<Ban>("/ban", payload: ban);


        /// <summary>
        /// Gets the ban
        /// </summary>
        /// <param name="ticket"></param>
        /// <returns></returns>
        public async Task<Response<Ban>> GetBanAsync(long ticket) => await GetRequestAsync<Ban>($"/ban/{ticket}");


        /// <summary>
        /// Deletes the ban
        /// </summary>
        /// <param name="ticket"></param>
        /// <returns></returns>
        public async Task<Response<bool>> DeleteBanAsync(long ticket) => await DeleteRequestAsync<bool>($"/ban/{ticket}");

        #endregion

        #region Account
        public async Task<Response<Account>> GetAccountAsync(string name) => await GetRequestAsync<Account>($"/account/{name}");

        public async Task<Response<Account>> CreateAccountAsync(Account account)
        {
            if (account is null)
                throw new ArgumentNullException(nameof(account));

            return await PostRequestAsync<Account>($"/account", payload: account);
        }

        public async Task<Response<Account>> UpdateAccountAsync(string name, Account account) => await PutRequestAsync<Account>($"/account/{name}", payload: account);

        public async Task<Response<bool>> DeleteAccountAsync(string name) => await DeleteRequestAsync<bool>($"/account/{name}");

        #endregion

        /// <summary>
        /// Gets the statistics of the server
        /// </summary>
        /// <returns></returns>
        public async Task<Response<Statistics>> GetStatisticsAsync() => await GetRequestAsync<Statistics>("/server/statistics");

        #region Request Fetching
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
            Uri url = BuildUrl(endpoint, queries);
            var _client = _httpClient;
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", _authorization);
            var response = await _client.GetAsync(url);
            return await ProcessResponseMessage<T>(response);
        }
        
        /// <summary>
        /// Sends a DELETE request to a specific endpoint
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="queries"></param>
        /// <returns></returns>
        private async Task<Response<T>> DeleteRequestAsync<T>(string endpoint, IEnumerable<KeyValuePair<string, object>> queries = null)
        {
            Uri url = BuildUrl(endpoint, queries);
            var _client = _httpClient;
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", _authorization);
            var response = await _client.DeleteAsync(url);
            return await ProcessResponseMessage<T>(response);
        }
        
        /// <summary>
        /// Sends a PUT request to a specific endpoint
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="queries"></param>
        /// <returns></returns>
        private async Task<Response<T>> PutRequestAsync<T>(string endpoint, IEnumerable<KeyValuePair<string, object>> queries = null, object payload = null)
        {
            Uri url = BuildUrl(endpoint, queries);
            var json = JsonConvert.SerializeObject(payload);
            var _client = _httpClient;
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", _authorization);
            var request = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PutAsync(url, request);
            return await ProcessResponseMessage<T>(response);
        }

        /// <summary>
        /// Sends a POST request to a specific endpoint
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="queries"></param>
        /// <returns></returns>
        private async Task<Response<T>> PostRequestAsync<T>(string endpoint, IEnumerable<KeyValuePair<string, object>> queries = null, object payload = null)
        {
            Uri url = BuildUrl(endpoint, queries);
            var json = payload == null ? "{}" : JsonConvert.SerializeObject(payload);
            var _client = _httpClient;
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", _authorization);

            var request = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(url, request);
            return await ProcessResponseMessage<T>(response);
        }

        private async Task<Response<T>> ProcessResponseMessage<T>(HttpResponseMessage response)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new HttpRequestException("Forbidden");


            //Read the json
            string json = await response.Content.ReadAsStringAsync();

            //Return the json object deserialized.
            var res = JsonConvert.DeserializeObject<Response<JToken>>(json);
            if (res == null) throw new Exception("Failed to get any response from the server. Got: " + json);

            switch (res.Status)
            {
                case RestStatus.Forbidden:
                    throw new RestForbiddenException(res);

                case RestStatus.RouteNotFound:
                    throw new RestRouteNotFoundException(res);

                case RestStatus.NotImplemented:
                    throw new NotImplementedException("Endpoint is not implemented.");

                case RestStatus.TooManyRequests:
                    throw new RestRateLimitException(new Response<RateLimit>(res));

                //Return the response. Everything else can be handled.
                default:
                    return new Response<T>(res);
            }
        }

        private Uri BuildUrl(string endpoint, IEnumerable<KeyValuePair<string, object>> queries)
        {

            string url = $"{Host}/api{endpoint.TrimEnd('/')}";
            if (queries != null)
            {
                var q = string.Join("&", queries.Select(kp => $"{kp.Key}={HttpUtility.UrlEncode(kp.Value.ToString())}"));
                if (!string.IsNullOrEmpty(q)) url += $"?{q}";
            }

            return new Uri(url);
        }
        #endregion
    }
}
