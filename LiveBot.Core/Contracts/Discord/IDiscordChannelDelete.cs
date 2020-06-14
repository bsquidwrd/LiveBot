namespace LiveBot.Core.Contracts.Discord
{
    public interface IDiscordChannelDelete
    {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
    }
}