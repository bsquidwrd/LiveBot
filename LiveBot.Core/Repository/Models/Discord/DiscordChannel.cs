namespace LiveBot.Core.Repository.Models.Discord
{
    public class DiscordChannel : BaseDiscordModel<DiscordChannel>
    {
        public DiscordGuild DiscordGuild { get; set; }
    }
}