using LiveBot.Core.Repository.Static;

namespace LiveBot.Core.Contracts
{
    public interface IMonitorRefreshAuth
    {
        public ServiceEnum ServiceType { get; set; }
        public string ClientId { get; set; }
    }
}