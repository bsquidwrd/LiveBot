using System.Collections.Generic;
using LiveBot.Core.Repository.Models.Streams;
using Newtonsoft.Json;

namespace LiveBot.Core.Repository.Models.Discord
{
    public class DiscordChannel : BaseDiscordModel<DiscordChannel>
    {
        public virtual DiscordGuild DiscordGuild { get; set; }

        [JsonIgnore]
        public virtual ICollection<StreamSubscription> StreamSubscriptions { get; set; }
    }
}