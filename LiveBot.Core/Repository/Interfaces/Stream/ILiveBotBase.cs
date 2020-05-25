using LiveBot.Core.Repository.Enums;

namespace LiveBot.Core.Repository.Interfaces.Stream
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