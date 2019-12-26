namespace LiveBot.Core.Repository.Models
{
    public class DiscordChannel : BaseModel<DiscordChannel>
    {
        public DiscordGuild DiscordGuild { get; set; }
    }
}