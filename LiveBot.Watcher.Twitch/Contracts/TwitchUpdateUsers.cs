using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Static;

namespace LiveBot.Watcher.Twitch.Contracts
{
    public class TwitchUpdateUsers : IMonitorUpdateUsers
    {
        public ServiceEnum ServiceType { get; set; }
    }
}