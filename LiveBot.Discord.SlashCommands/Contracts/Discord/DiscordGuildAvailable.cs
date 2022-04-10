using LiveBot.Core.Contracts.Discord;

#nullable disable

namespace LiveBot.Discord.SlashCommands.Contracts.Discord
{
    public class DiscordGuildAvailable : IDiscordGuildAvailable
    {
        public ulong GuildId { get; set; }
        public string GuildName { get; set; }
        public string IconUrl { get; set; }
    }
}