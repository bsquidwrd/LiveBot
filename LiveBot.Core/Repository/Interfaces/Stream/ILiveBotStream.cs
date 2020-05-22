using System;

namespace LiveBot.Core.Repository.Interfaces.Stream
{
    public interface ILiveBotStream
    {
        public string ServiceName { get; }
        public ILiveBotUser User { get; }
        public ILiveBotGame Game { get; }
        public string Id { get; }
        public string Title { get; }
        public DateTime StartTime { get; }
        public string Language { get; }
        public string Thumbnail { get; }
    }
}