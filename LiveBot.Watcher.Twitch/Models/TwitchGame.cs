using LiveBot.Core.Repository.Interfaces.Stream;
using TwitchLib.Api.Helix.Models.Games;

namespace LiveBot.Watcher.Twitch.Models
{
    public class TwitchGame : BaseLiveBotGame
    {
        public TwitchGame(Game game)
        {
            Id = game.Id;
            Name = game.Name;
            Image = game.BoxArtUrl;
        }
    }
}