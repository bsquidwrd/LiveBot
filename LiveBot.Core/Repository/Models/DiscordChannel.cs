namespace LiveBot.Core.Repository.Models
{
    public class DiscordChannel : BaseDiscordModel<DiscordChannel>
    {
        public DiscordGuild DiscordGuild { get; set; }
    }
}