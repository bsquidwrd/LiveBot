namespace LiveBot.Core.Repository.Models.Discord
{
    public class DiscordGuildConfig : BaseModel<DiscordGuildConfig>
    {
        public string Message { get; set; }
        public long DiscordGuildId { get; set; }
        public virtual DiscordGuild DiscordGuild { get; set; }
        public virtual DiscordChannel DiscordChannel { get; set; }
        public virtual DiscordRole DiscordRole { get; set; }
        public virtual DiscordRole MonitorRole { get; set; }
    }
}