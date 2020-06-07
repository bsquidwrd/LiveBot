using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Static;

namespace LiveBot.Core.Repository.Base.Monitor
{
    /// <summary>
    /// Base implementation of <c>ILiveBotGame</c>
    /// </summary>
    public abstract class BaseLiveBotGame : BaseLiveBot, ILiveBotGame
    {
        public BaseLiveBotGame(string baseURL, ServiceEnum serviceType) : base(baseURL, serviceType)
        {
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string ThumbnailURL { get; set; }

        public override string ToString()
        {
            return $"{ServiceType}: {Name}";
        }
    }
}