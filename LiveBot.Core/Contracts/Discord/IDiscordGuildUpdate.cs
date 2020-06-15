namespace LiveBot.Core.Contracts.Discord
{
    public interface IDiscordGuildUpdate
    {
        public ulong GuildId { get; set; }
        public string GuildName { get; set; }
    }
}