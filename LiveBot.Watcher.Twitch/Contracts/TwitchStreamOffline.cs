using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces.Monitor;

#nullable disable

namespace LiveBot.Watcher.Twitch.Contracts
{
    public class TwitchStreamOffline : IStreamOffline
    {
        public ILiveBotStream Stream { get; set; }
    }
}