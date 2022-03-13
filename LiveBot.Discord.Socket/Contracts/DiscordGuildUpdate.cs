
using LiveBot.Core.Contracts.Discord;

namespace LiveBot.Discord.Socket.Contracts
{
    public class DiscordGuildUpdate : IDiscordGuildUpdate
    {
        public ulong GuildId { get; set; }
        public string GuildName { get; set; }
        public string IconUrl { get; set; }
    }
}