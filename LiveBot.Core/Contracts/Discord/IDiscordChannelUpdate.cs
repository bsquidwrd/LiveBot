namespace LiveBot.Core.Contracts.Discord
{
    public interface IDiscordChannelUpdate
    {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
    }
}