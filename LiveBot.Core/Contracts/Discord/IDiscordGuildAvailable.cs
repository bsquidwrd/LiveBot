namespace LiveBot.Core.Contracts.Discord
{
    public interface IDiscordGuildAvailable
    {
        public ulong GuildId { get; set; }
        public string GuildName { get; set; }
        public string IconUrl { get; set; }
    }
}