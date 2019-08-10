using DSharpPlus.Entities;
using KoalaBot.Entities;
using KoalaBot.Extensions;
using KoalaBot.Logging;
using KoalaBot.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Managers
{
    public class BoostEmojiManager : Manager
    {
        public BoostEmojiManager(Koala bot, Logger logger = null) : base(bot, logger)
        {
            Discord.GuildMemberRemoved += async (args) =>
            {
                await DeleteEmojiAsync(args.Member);
            };

            Discord.GuildMemberUpdated += async (args) =>
            {
                if (!args.Member.PremiumSince.HasValue)
                    await DeleteEmojiAsync(args.Member);
            };
        }

        public async Task SetEmojiAsync(DiscordMember member, string url)
        {
            //Validate the user is allowed to
            if (!member.PremiumSince.HasValue)
                throw new ArgumentException("The member hasn't boosted the server.");

            string tmp = Path.GetTempFileName();
            try
            {
                BoostEmoji be = new BoostEmoji(member)
                {
                    URL = url,
                    Name = member.Username,
                };

                using (WebClient client = new WebClient())
                    await client.DownloadFileTaskAsync(new Uri(url), tmp);

                using (FileStream fs = new FileStream(tmp, FileMode.Open))
                {
                    var emoji = await member.Guild.CreateEmojiAsync(be.Name, fs);
                    be.EmojiId = emoji.Id;
                }

                await be.SaveAsync(DbContext);
            }
            finally
            {
                //Finally delete the temp
                Logger.Log("Deleting the temp: " + tmp);
                File.Delete(tmp);
            }
        }

        public async Task DeleteEmojiAsync(DiscordMember member)
        {
            BoostEmoji be = new BoostEmoji(member);
            if (!await be.LoadAsync(DbContext))
                throw new ArgumentNullException("Member does not have an emoji set.");

            //Delete it and then remove its thingy
            Logger.Log("Deleting {0}'s emoji", member);
            await be.DeleteAsync(DbContext);

            //Remove from the server
            DiscordGuildEmoji emoji = await member.Guild.GetEmojiAsync(be.EmojiId);
            await member.Guild.DeleteEmojiAsync(emoji, "Unboosted.");
        }
        
    }
}
