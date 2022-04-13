using System;

namespace LiveBot.Core.Contracts.Discord
{
    public interface IDiscordMemberLive
    {
        public ulong DiscordUserId { get; set; }
        public string Url { get; set; }
        public string GameName { get; set; }
        public string GameDetails { get; set; }
        public DateTime LiveTime { get; set; }
    }
}