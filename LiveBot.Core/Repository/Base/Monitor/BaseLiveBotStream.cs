using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Static;
using System;

namespace LiveBot.Core.Repository.Base.Monitor
{
    /// <summary>
    /// Base implementation of <c>ILiveBotStream</c>
    /// </summary>
    public abstract class BaseLiveBotStream : BaseLiveBot, ILiveBotStream
    {
        public BaseLiveBotStream(string serviceName, string baseURL, ServiceEnum serviceType) : base(serviceName, baseURL, serviceType)
        {
        }

        public ILiveBotUser User { get; set; }

        public ILiveBotGame Game { get; set; }

        public string Id { get; set; }

        public string Title { get; set; }

        public DateTime StartTime { get; set; }

        public string ThumbnailURL { get; set; }

        public abstract string GetStreamURL();

        public override string ToString()
        {
            return $"{ServiceType.ToString()}: {Title}";
        }
    }
}