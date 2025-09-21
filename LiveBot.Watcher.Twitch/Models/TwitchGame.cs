using LiveBot.Core.Repository.Base.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;
using TwitchLib.Api.Helix.Models.Games;

namespace LiveBot.Watcher.Twitch.Models
{
    /// <summary>
    /// Represents a Twitch Game
    /// </summary>
    public class TwitchGame : BaseLiveBotGame
    {
        public TwitchGame(string baseURL, ServiceEnum serviceType, Game? game = null) : base(baseURL, serviceType)
        {
            if (game == null)
            {
                Id = "0";
                Name = "[Not Set]";
                ThumbnailURL = "";
            }
            else if (string.IsNullOrEmpty(game.Id))
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

        public TwitchGame(string baseURL, ServiceEnum serviceType, StreamGame? game = null) : base(baseURL, serviceType)
        {
            if (game == null)
            {
                Id = "0";
                Name = "[Not Set]";
                ThumbnailURL = "";
            }
            else
            {
                Id = game.SourceId;
                Name = game.Name;
                ThumbnailURL = game.ThumbnailURL;
            }
        }

        public TwitchGame()
        { }
    }
}