using LiveBot.Core.Repository.Interfaces.Monitor;

namespace LiveBot.Messaging.Services
{
    public class StreamUpdated
    {
        public ILiveBotStream Stream { get; set; }
    }
}