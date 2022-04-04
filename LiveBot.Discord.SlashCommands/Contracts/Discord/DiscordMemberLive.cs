using LiveBot.Core.Contracts.Discord;

namespace LiveBot.Discord.SlashCommands.Contracts.Discord
{
    public class DiscordMemberLive : IDiscordMemberLive
    {
        public string Url { get; set; }
        public ulong DiscordGuildId { get; set; }
        public ulong DiscordUserId { get; set; }
    }
}