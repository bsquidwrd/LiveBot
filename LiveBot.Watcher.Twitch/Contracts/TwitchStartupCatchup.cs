using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Static;

namespace LiveBot.Watcher.Twitch.Contracts
{
    /// <summary>
    /// Twitch implementation of startup catch-up
    /// </summary>
    public class TwitchStartupCatchup : IStartupCatchup
    {
        public ServiceEnum ServiceType { get; set; } = ServiceEnum.Twitch;
        public IEnumerable<ILiveBotUser> StreamUsers { get; set; } = new List<ILiveBotUser>();
    }
}