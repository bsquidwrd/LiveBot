using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces.Monitor;

namespace LiveBot.Watcher.Twitch.Contracts
{
    /// <summary>
    /// Twitch implementation of startup catch-up
    /// </summary>
    public class TwitchStartupCatchup : IStartupCatchup
    {
        public string ServiceType { get; set; } = "Twitch";
        public IEnumerable<ILiveBotUser> StreamUsers { get; set; } = new List<ILiveBotUser>();
    }
}