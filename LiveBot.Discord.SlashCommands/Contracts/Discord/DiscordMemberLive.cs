﻿using LiveBot.Core.Contracts.Discord;

#nullable disable

namespace LiveBot.Discord.SlashCommands.Contracts.Discord
{
    public class DiscordMemberLive : IDiscordMemberLive
    {
        public ulong DiscordUserId { get; set; }
        public string Url { get; set; }
        public string GameName { get; set; }
        public string GameDetails { get; set; }

        public DateTime LiveTime { get; set; } = DateTime.UtcNow;
    }
}