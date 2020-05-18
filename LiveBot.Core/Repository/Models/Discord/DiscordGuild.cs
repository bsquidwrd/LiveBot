﻿using System.Collections.Generic;

namespace LiveBot.Core.Repository.Models.Discord
{
    public class DiscordGuild : BaseDiscordModel<DiscordGuild>
    {
        public virtual ICollection<DiscordChannel> DiscordChannels { get; set; }
    }
}