using LiveBot.Core.Repository.Interfaces.Monitor;

namespace LiveBot.Core.Contracts
{
    public interface IStreamOffline
    {
        public ILiveBotStream Stream { get; set; }
    }
}