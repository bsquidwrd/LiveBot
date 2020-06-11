using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces.Monitor;

namespace LiveBot.Watcher.Twitch.Contracts
{
    public class TwitchStreamUpdate : IStreamUpdate
    {
        public ILiveBotStream Stream { get; set; }
    }
}