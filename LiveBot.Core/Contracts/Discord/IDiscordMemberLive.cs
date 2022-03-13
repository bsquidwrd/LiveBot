namespace LiveBot.Core.Contracts.Discord
{
    public interface IDiscordMemberLive
    {
        public string Url { get; set; }
        public ulong DiscordGuildId { get; set; }
        public ulong DiscordUserId { get; set; }
    }
}