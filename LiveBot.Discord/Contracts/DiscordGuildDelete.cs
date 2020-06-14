using LiveBot.Core.Contracts.Discord;

namespace LiveBot.Discord.Contracts
{
    public class DiscordGuildDelete : IDiscordGuildDelete
    {
        public ulong GuildId { get; set; }
    }
}