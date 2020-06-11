using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Static;

namespace LiveBot.Watcher.Twitch.Contracts
{
    public class TwitchRefreshAuth : IMonitorRefreshAuth
    {
        public ServiceEnum ServiceType { get; set; }
        public string ClientId { get; set; }
    }
}