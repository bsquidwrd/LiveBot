using LiveBot.Core.Repository.Base.Monitor;
using LiveBot.Core.Repository.Enums;
using TwitchLib.Api.Helix.Models.Games;

namespace LiveBot.Watcher.Twitch.Models
{
    /// <summary>
    /// Represents a Twitch Game
    /// </summary>
    public class TwitchGame : BaseLiveBotGame
    {
        public TwitchGame(string serviceName, string baseURL, ServiceEnum serviceType, Game game = null) : base(serviceName, baseURL, serviceType)
        {
            if (game == null)
            {
                Id = "0";
                Name = "[Not Set]";
                ThumbnailURL = "";
            }
            else
            {
                Id = game.Id;
                Name = game.Name;
                ThumbnailURL = game.BoxArtUrl;
            }
        }
    }
}