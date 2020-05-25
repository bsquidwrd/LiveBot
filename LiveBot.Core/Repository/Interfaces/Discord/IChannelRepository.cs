using LiveBot.Core.Repository.Models.Discord;

namespace LiveBot.Core.Repository.Interfaces.Discord
{
    /// <summary>
    /// Interface for Database interaction with a Discord Channel
    /// </summary>
    public interface IChannelRepository : IRepository<DiscordChannel>
    {
    }
}