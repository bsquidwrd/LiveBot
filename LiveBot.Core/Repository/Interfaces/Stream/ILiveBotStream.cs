using System;

namespace LiveBot.Core.Repository.Interfaces.Stream
{
    public interface ILiveBotStream : ILiveBotBase
    {
        public string BaseURL { get; }
        public ILiveBotUser User { get; }
        public ILiveBotGame Game { get; }
        public string Id { get; }
        public string Title { get; }
        public DateTime StartTime { get; }
        public string ThumbnailURL { get; }

        public string GetStreamURL();
    }
}