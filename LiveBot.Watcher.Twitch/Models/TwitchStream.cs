using LiveBot.Core.Repository.Base.Stream;
using LiveBot.Core.Repository.Enums;
using LiveBot.Core.Repository.Interfaces.Stream;
using TwitchLib.Api.Helix.Models.Streams;

namespace LiveBot.Watcher.Twitch.Models
{
    public class TwitchStream : BaseLiveBotStream
    {
        public TwitchStream(string baseURL, ServiceEnum serviceType, Stream stream, ILiveBotUser user, ILiveBotGame game) : base(baseURL, serviceType)
        {
            User = user;
            Id = stream.Id;
            Title = stream.Title;
            StartTime = stream.StartedAt;
            Game = game;
            ThumbnailURL = stream.ThumbnailUrl;
        }

        public override string GetStreamURL()
        {
            return $@"{User.GetProfileURL()}";
        }
    }
}