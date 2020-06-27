using LiveBot.Core.Repository.Models.Streams;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace LiveBot.Core.Repository.Models.Discord
{
    public class DiscordRole : BaseDiscordModel<DiscordRole>
    {
        public virtual DiscordGuild DiscordGuild { get; set; }

        [JsonIgnore]
        public virtual ICollection<StreamSubscription> StreamSubscriptions { get; set; }
    }
}