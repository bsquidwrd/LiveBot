using LiveBot.Core.Contracts.Discord;

namespace LiveBot.Discord.SlashCommands.Contracts.Discord
{
    public class DiscordRoleDelete : IDiscordRoleDelete
    {
        public ulong GuildId { get; set; }
        public ulong RoleId { get; set; }
    }
}