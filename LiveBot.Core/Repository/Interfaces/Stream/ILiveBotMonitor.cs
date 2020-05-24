using System.Threading.Tasks;

namespace LiveBot.Core.Repository.Interfaces.Stream
{
    public interface ILiveBotMonitor : ILiveBotBase
    {
        public string URLPattern { get; set; }

        public ILiveBotMonitorStart GetStartClass();

        public Task<ILiveBotUser> GetUser(string username = null, string userId = null);

        public Task<ILiveBotStream> GetStream(ILiveBotUser user);

        public Task<ILiveBotGame> GetGame(string gameId);
        public bool IsValid(string streamURL);
    }
}