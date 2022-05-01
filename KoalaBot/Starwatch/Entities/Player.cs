using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Linq;

namespace KoalaBot.Starwatch.Entities
{
    public class Player
    {
        public int? Connection { get; set; } = null;
        public string Username { get; set; } = null;
        public string Nickname { get; set; } = null;
        public string AccountName { get; set; } = null;
        public string UUID { get; set; } = null;
        public string IP { get; set; } = null;

        public string EncodeAsParameters()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            if (!(Connection is null))
                parameters.Add("cid", Connection.ToString());

            if (!(Username is null))
                parameters.Add("username", Username);

            if (!(Nickname is null))
                parameters.Add("nickname", Nickname);

            if (!(AccountName is null))
                parameters.Add("account", AccountName);

            if (!(UUID is null))
                parameters.Add("uuid", UUID);

            if (!(IP is null))
                parameters.Add("ip", IP);

            if (parameters.Count == 0)
                return "";

            return string.Join("&", parameters.Select(kvp => $"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}"));
        }
    }
}
