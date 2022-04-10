namespace LiveBot.Core.Contracts.Discord
{
    public interface IDiscordMemberLive
    {
        public ulong DiscordGuildId { get; set; }
        public ulong DiscordUserId { get; set; }
        public string Url { get; set; }
        public string GameName { get; set; }
        public string GameDetails { get; set; }
    }
}