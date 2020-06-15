using LiveBot.Core.Repository.Models.Streams;
using System.Collections.Generic;

namespace LiveBot.Core.Repository.Models.Discord
{
    public class DiscordRole : BaseDiscordModel<DiscordRole>
    {
        public virtual DiscordGuild DiscordGuild { get; set; }
        public virtual ICollection<StreamSubscription> StreamSubscriptions { get; set; }
    }
}