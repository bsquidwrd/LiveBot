using LiveBot.Core.Contracts.Discord;

namespace LiveBot.Discord.Contracts
{
    public class DiscordGuildAvailable : IDiscordGuildAvailable
    {
        public ulong GuildId { get; set; }
        public string GuildName { get; set; }
        public string IconUrl { get; set; }
    }
}