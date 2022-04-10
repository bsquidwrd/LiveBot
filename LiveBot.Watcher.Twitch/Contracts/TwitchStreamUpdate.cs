using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces.Monitor;

#nullable disable

namespace LiveBot.Watcher.Twitch.Contracts
{
    public class TwitchStreamUpdate : IStreamUpdate
    {
        public ILiveBotStream Stream { get; set; }
    }
}