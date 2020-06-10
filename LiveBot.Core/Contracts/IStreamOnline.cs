using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;

namespace LiveBot.Core.Contracts
{
    public interface IStreamOnline
    {
        public ILiveBotStream Stream { get; set; }
    }
}