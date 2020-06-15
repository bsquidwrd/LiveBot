using LiveBot.Core.Contracts.Discord;

namespace LiveBot.Discord.Contracts
{
    public class DiscordAlertChannel : IDiscordAlertChannel
    {
        public string Message { get; set; }
        public ulong ChannelId { get; set; }
    }
}