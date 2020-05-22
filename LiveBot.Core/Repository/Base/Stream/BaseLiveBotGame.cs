using LiveBot.Core.Repository.Enums;
using LiveBot.Core.Repository.Interfaces.Stream;

namespace LiveBot.Core.Repository.Base.Stream
{
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
            return $@"{ServiceType.ToString()}: {Name}";
        }
    }
}