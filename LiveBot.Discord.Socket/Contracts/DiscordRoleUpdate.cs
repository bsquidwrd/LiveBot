using LiveBot.Core.Contracts.Discord;

namespace LiveBot.Discord.Socket.Contracts
{
    public class DiscordRoleUpdate : IDiscordRoleUpdate
    {
        public ulong GuildId { get; set; }
        public ulong RoleId { get; set; }
        public string RoleName { get; set; }
    }
}