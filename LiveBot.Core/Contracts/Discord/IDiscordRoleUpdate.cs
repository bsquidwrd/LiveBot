namespace LiveBot.Core.Contracts.Discord
{
    public interface IDiscordRoleUpdate
    {
        public ulong GuildId { get; set; }
        public ulong RoleId { get; set; }
    }
}