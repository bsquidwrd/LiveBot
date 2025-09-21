using System.Collections.Generic;
using LiveBot.Core.Repository.Models.Streams;
using Newtonsoft.Json;

namespace LiveBot.Core.Repository.Models.Discord
{
    public class DiscordGuild : BaseDiscordModel<DiscordGuild>
    {
        public string IconUrl { get; set; }
        public virtual DiscordGuildConfig Config { get; set; }

        public bool IsInBeta { get; set; }

        [JsonIgnore]
        public virtual ICollection<DiscordChannel> DiscordChannels { get; set; }

        [JsonIgnore]
        public virtual ICollection<StreamSubscription> StreamSubscriptions { get; set; }
    }
}