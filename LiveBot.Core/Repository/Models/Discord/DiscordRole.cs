namespace LiveBot.Core.Repository.Models.Discord
{
    public class DiscordRole : BaseDiscordModel<DiscordRole>
    {
        public virtual DiscordGuild DiscordGuild { get; set; }
    }
}