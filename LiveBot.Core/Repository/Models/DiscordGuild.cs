using System.Collections.Generic;

namespace LiveBot.Core.Repository.Models
{
    public class DiscordGuild : BaseModel<DiscordGuild>
    {
        public virtual ICollection<DiscordChannel> DiscordChannels { get; set; }
    }
}