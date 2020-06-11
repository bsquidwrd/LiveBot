using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Static;

namespace LiveBot.Core.Contracts
{
    public interface IMonitorUpdateUser
    {
        public ServiceEnum ServiceType { get; set; }
        public ILiveBotUser User { get; set; }
    }
}