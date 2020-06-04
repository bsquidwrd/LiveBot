using LiveBot.Core.Repository.Enums;

namespace LiveBot.Core.Repository.Interfaces.Monitor
{
    /// <summary>
    /// General base of all bot classes
    /// </summary>
    public interface ILiveBotBase
    {
        public string ServiceName { get; set; }
        public string BaseURL { get; set; }
        public ServiceEnum ServiceType { get; set; }
    }
}