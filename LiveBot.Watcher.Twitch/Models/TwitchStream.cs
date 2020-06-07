using LiveBot.Core.Repository.Base.Monitor;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Static;
using TwitchLib.Api.Helix.Models.Streams;

namespace LiveBot.Watcher.Twitch.Models
{
    /// <summary>
    /// Represents a Twitch Stream
    /// </summary>
    public class TwitchStream : BaseLiveBotStream
    {
        public TwitchStream(string serviceName, string baseURL, ServiceEnum serviceType, Stream stream, ILiveBotUser user, ILiveBotGame game) : base(serviceName, baseURL, serviceType)
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
            return $"{User.GetProfileURL()}";
        }
    }
}