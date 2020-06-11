using LiveBot.Core.Repository.Interfaces.Monitor;

namespace LiveBot.Core.Contracts
{
    public interface IStreamUpdate
    {
        public ILiveBotStream Stream { get; set; }
    }
}