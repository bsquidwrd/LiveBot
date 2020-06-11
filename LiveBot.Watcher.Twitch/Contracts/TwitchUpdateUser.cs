using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Static;

namespace LiveBot.Watcher.Twitch.Contracts
{
    public class TwitchUpdateUser : IMonitorUpdateUser
    {
        public ServiceEnum ServiceType { get; set; }
        public ILiveBotUser User { get; set; }
        public TwitchUpdateUser(ServiceEnum serviceType, ILiveBotUser user)
        {
            ServiceType = serviceType;
            User = user;
        }
    }
}