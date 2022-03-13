using LiveBot.Core.Contracts.Discord;

namespace LiveBot.Discord.Socket.Contracts
{
    public class DiscordGuildDelete : IDiscordGuildDelete
    {
        public ulong GuildId { get; set; }
    }
}