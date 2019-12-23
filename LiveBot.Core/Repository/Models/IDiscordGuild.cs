namespace LiveBot.Core.Repository.Models
{
    public interface IDiscordGuild
    {
        public ulong Id { get; set; }
        public string Name { get; set; }
    }
}