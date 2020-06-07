using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Static;

namespace LiveBot.Core.Repository.Base.Monitor
{
    /// <summary>
    /// Base implementation of <c>ILiveBotBase</c>
    /// </summary>
    public abstract class BaseLiveBot : ILiveBotBase
    {
        public BaseLiveBot(string serviceName, string baseURL, ServiceEnum serviceType)
        {
            ServiceName = serviceName;
            BaseURL = baseURL;
            ServiceType = serviceType;
        }

        public string ServiceName { get; set; }
        public string BaseURL { get; set; }
        public ServiceEnum ServiceType { get; set; }
    }
}