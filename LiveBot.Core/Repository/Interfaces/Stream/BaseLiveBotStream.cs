using System;

namespace LiveBot.Core.Repository.Interfaces.Stream
{
    public abstract class BaseLiveBotStream : ILiveBotStream
    {
        public BaseLiveBotStream()
        {
        }

        public string ServiceName { get; set; }

        public ILiveBotUser User { get; set; }

        public ILiveBotGame Game { get; set; }

        public string Id { get; set; }

        public string Title { get; set; }

        public DateTime StartTime { get; set; }

        public string Language { get; set; }

        public string Thumbnail { get; set; }

        public override string ToString()
        {
            return $@"{Title}";
        }
    }
}