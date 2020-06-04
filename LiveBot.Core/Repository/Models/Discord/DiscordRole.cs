namespace LiveBot.Core.Repository.Models.Discord
{
    public class DiscordRole : BaseDiscordModel<DiscordRole>
    {
        public DiscordGuild DiscordGuild { get; set; }
    }
}