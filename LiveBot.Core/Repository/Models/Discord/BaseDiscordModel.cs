namespace LiveBot.Core.Repository.Models.Discord
{
    public abstract class BaseDiscordModel<T> : BaseModel<T>
        where T : BaseDiscordModel<T>
    {
        public ulong DiscordId { get; set; }
        public string Name { get; set; }
    }
}