﻿using LiveBot.Core.Contracts.Discord;

namespace LiveBot.Discord.Contracts
{
    public class DiscordRoleDelete : IDiscordRoleDelete
    {
        public ulong GuildId { get; set; }
        public ulong RoleId { get; set; }
    }
}