using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Static;

namespace LiveBot.Discord.Contracts
{
    public class DiscordMemberLive : IDiscordMemberLive
    {
        public ServiceEnum ServiceType { get; set; }
        public string Url { get; set; }
        public ulong DiscordGuildId { get; set; }
        public ulong DiscordUserId { get; set; }
    }
}