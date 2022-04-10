namespace LiveBot.Core.Repository.Models.Discord
{
    public class DiscordGuildConfig : BaseModel<DiscordGuildConfig>
    {
        public string Message { get; set; }
        public long DiscordGuildId { get; set; }
        public virtual DiscordGuild DiscordGuild { get; set; }
        public virtual DiscordChannel DiscordChannel { get; set; }
        public virtual ulong? MentionRoleDiscordId { get; set; }
        public virtual ulong? MonitorRoleDiscordId { get; set; }
        public virtual ulong? AdminRoleDiscordId { get; set; }
    }
}