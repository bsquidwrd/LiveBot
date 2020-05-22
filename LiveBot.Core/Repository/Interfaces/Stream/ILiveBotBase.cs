using LiveBot.Core.Repository.Enums;

namespace LiveBot.Core.Repository.Interfaces.Stream
{
    public interface ILiveBotBase
    {
        public string BaseURL { get; set; }
        public ServiceEnum ServiceType { get; set; }
    }
}