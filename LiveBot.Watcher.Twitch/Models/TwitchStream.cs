using LiveBot.Core.Repository.Interfaces.Stream;
using TwitchLib.Api.Helix.Models.Streams;

namespace LiveBot.Watcher.Twitch.Models
{
    public class TwitchStream: BaseLiveBotStream
    {

        public TwitchStream(Stream stream, ILiveBotUser user, ILiveBotGame game)
        {
            ServiceName = "Twitch";
            User = user;
            Id = stream.Id;
            Title = stream.Title;
            StartTime = stream.StartedAt;
            Language = stream.Language;
            Game = game;
            Thumbnail = stream.ThumbnailUrl;
        }
    }
}