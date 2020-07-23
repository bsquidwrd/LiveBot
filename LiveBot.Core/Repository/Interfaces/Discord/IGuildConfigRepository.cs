using LiveBot.Core.Repository.Models.Discord;

namespace LiveBot.Core.Repository.Interfaces.Discord
{
    /// <summary>
    /// Interface for Database interaction with a Discord Guild Config
    /// </summary>
    public interface IGuildConfigRepository : IRepository<DiscordGuildConfig>
    {
    }
}