using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Starwatch.Entities
{
    public class Session
    {
        public long Id { get; set; }
        public long UptimeId { get; set; }
        
        public int Connection { get; set; }
        public string IP { get; set; }
        public string UUID { get; set; }
        public string Username { get; set; }
        public string Account { get; set; }

        public DateTime ConnectedAt { get; set; }
        public DateTime? DisconnectedAt { get; set; }

        public DiscordEmbedBuilder GetEmbedBuilder()
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.WithTitle("Session #" + Id)
                .AddField("Character", Username, true)
                .AddField("Account", Account, true)
                .AddField("IP", IP, true)
                .AddField("UUID", UUID, false)
                .AddField("Connected", ConnectedAt.ToString("dd / MM / yyyy   h:mm tt"), true);

            if (DisconnectedAt.HasValue)
                builder.AddField("Disconnected", DisconnectedAt.Value.ToString("dd / MM / yyyy   h:mm tt"), true);

            return builder;
        }
    }
}
