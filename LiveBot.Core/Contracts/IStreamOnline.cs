using LiveBot.Core.Repository.Interfaces.Monitor;

namespace LiveBot.Core.Contracts
{
    public interface IStreamOnline
    {
        public ILiveBotStream Stream { get; set; }
    }
}