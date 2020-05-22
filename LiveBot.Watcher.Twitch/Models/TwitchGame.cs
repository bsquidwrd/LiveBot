using LiveBot.Core.Repository.Base.Stream;
using LiveBot.Core.Repository.Enums;
using TwitchLib.Api.Helix.Models.Games;

namespace LiveBot.Watcher.Twitch.Models
{
    public class TwitchGame : BaseLiveBotGame
    {
        public TwitchGame(string baseURL, ServiceEnum serviceType, Game game) : base(baseURL, serviceType)
        {
            Id = game.Id;
            Name = game.Name;
            ThumbnailURL = game.BoxArtUrl;
        }
    }
}