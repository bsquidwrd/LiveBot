using LiveBot.Core.Repository.Interfaces.Monitor;

namespace LiveBot.Core.Contracts
{
    public interface IStreamCheckResponse
    {
        public bool IsLive { get; set; }
        public ILiveBotStream? Stream { get; set; }
        public string? Error { get; set; }
    }
}

