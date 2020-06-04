using LiveBot.Core.Repository.Enums;
using LiveBot.Core.Repository.Interfaces.Monitor;

namespace LiveBot.Core.Repository.Base.Monitor
{
    /// <summary>
    /// Base implementation of <c>ILiveBotGame</c>
    /// </summary>
    public abstract class BaseLiveBotGame : BaseLiveBot, ILiveBotGame
    {
        public BaseLiveBotGame(string serviceName, string baseURL, ServiceEnum serviceType) : base(serviceName, baseURL, serviceType)
        {
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string ThumbnailURL { get; set; }

        public override string ToString()
        {
            return $"{ServiceType.ToString()}: {Name}";
        }
    }
}