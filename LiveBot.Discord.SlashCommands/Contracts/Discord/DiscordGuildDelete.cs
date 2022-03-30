using LiveBot.Core.Contracts.Discord;

namespace LiveBot.Discord.SlashCommands.Contracts.Discord
{
    public class DiscordGuildDelete : IDiscordGuildDelete
    {
        public ulong GuildId { get; set; }
    }
}