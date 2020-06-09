namespace LiveBot.Core.Repository.Models.Discord
{
    public class DiscordChannel : BaseDiscordModel<DiscordChannel>
    {
        public virtual DiscordGuild DiscordGuild { get; set; }
    }
}