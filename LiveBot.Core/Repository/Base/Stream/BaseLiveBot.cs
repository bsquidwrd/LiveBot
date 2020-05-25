using LiveBot.Core.Repository.Enums;
using LiveBot.Core.Repository.Interfaces.Stream;

namespace LiveBot.Core.Repository.Base.Stream
{
    /// <summary>
    /// Base implementation of <c>ILiveBotBase</c>
    /// </summary>
    public abstract class BaseLiveBot : ILiveBotBase
    {
        public BaseLiveBot(string baseURL, ServiceEnum serviceType)
        {
            BaseURL = baseURL;
            ServiceType = serviceType;
        }

        public string BaseURL { get; set; }
        public ServiceEnum ServiceType { get; set; }
    }
}