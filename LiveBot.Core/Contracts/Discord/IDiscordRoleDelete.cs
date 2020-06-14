namespace LiveBot.Core.Contracts.Discord
{
    public interface IDiscordRoleDelete
    {
        public ulong GuildId { get; set; }
        public ulong RoleId { get; set; }
    }
}