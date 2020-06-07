using LiveBot.Core.Repository.Static;

namespace LiveBot.Core.Repository.Interfaces.Monitor
{
    /// <summary>
    /// General base of all bot classes
    /// </summary>
    public interface ILiveBotBase
    {
        public string BaseURL { get; set; }
        public ServiceEnum ServiceType { get; set; }
    }
}