using LiveBot.Core.Repository.Models;

namespace LiveBot.Repository.Models
{
    public class DiscordGuild : IDiscordGuild
    {
        public ulong Id { get; set; }
        public string Name { get; set; }
    }
}