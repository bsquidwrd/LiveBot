using LiveBot.Core.Repository.Static;

namespace LiveBot.Core.Contracts
{
    public interface IStreamCheckRequest
    {
        public ServiceEnum ServiceType { get; set; }
        public string ProfileURL { get; set; }
    }
}