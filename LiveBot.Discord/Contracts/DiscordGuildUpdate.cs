using LiveBot.Core.Contracts.Discord;

namespace LiveBot.Discord.Contracts
{
    public class DiscordGuildUpdate : IDiscordGuildUpdate
    {
        public ulong GuildId { get; set; }
    }
}