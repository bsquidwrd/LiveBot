using LiveBot.Core.Contracts.Discord;

namespace LiveBot.Discord.Contracts
{
    public class DiscordChannelDelete : IDiscordChannelDelete
    {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
    }
}