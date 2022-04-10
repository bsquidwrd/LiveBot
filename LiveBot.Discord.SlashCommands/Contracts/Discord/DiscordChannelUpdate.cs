using LiveBot.Core.Contracts.Discord;

#nullable disable

namespace LiveBot.Discord.SlashCommands.Contracts.Discord
{
    public class DiscordChannelUpdate : IDiscordChannelUpdate
    {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public string ChannelName { get; set; }
    }
}