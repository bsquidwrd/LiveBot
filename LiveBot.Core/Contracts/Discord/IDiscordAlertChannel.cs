namespace LiveBot.Core.Contracts.Discord
{
    public interface IDiscordAlertChannel
    {
        public string Message { get; set; }
        public ulong ChannelId { get; set; }
    }
}