using LiveBot.Core.Contracts.Discord;

namespace LiveBot.Discord.Contracts
{
    public class DiscordChannelUpdate : IDiscordChannelUpdate
    {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public string ChannelName { get; set; }
    }
}