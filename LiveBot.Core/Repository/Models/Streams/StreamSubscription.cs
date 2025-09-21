using System.Collections.Generic;
using LiveBot.Core.Repository.Models.Discord;
using Newtonsoft.Json;

namespace LiveBot.Core.Repository.Models.Streams
{
    public class StreamSubscription : BaseModel<StreamSubscription>
    {
        public virtual StreamUser User { get; set; }
        public virtual DiscordGuild DiscordGuild { get; set; }
        public virtual DiscordChannel DiscordChannel { get; set; }
        public string Message { get; set; }

        [JsonIgnore]
        public virtual ICollection<RoleToMention> RolesToMention { get; set; }
    }
}