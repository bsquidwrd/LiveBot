using LiveBot.Core.Repository.Static;

namespace LiveBot.Core.Contracts.Discord
{
    public interface IDiscordMemberLive
    {
        public ServiceEnum ServiceType { get; set; }
        public string Url { get; set; }
        public ulong DiscordGuildId { get; set; }
        public ulong DiscordUserId { get; set; }
    }
}