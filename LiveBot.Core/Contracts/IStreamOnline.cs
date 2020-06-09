using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;

namespace LiveBot.Core.Contracts
{
    public interface IStreamOnline
    {
        public StreamSubscription Subscription { get; set; }
        public ILiveBotStream Stream { get; set; }
    }
}