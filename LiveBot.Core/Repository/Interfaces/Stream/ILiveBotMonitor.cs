using System;
using System.Threading.Tasks;

namespace LiveBot.Core.Repository.Interfaces.Stream
{
    public interface ILiveBotMonitor
    {
        public Task StartAsync();
        public Task _Stop();
        public Task<ILiveBotUser> GetUser(string username = null, string userId = null);
        public Task<ILiveBotStream> GetStream(ILiveBotUser user);
        public Task<ILiveBotGame> GetGame(string gameId);
    }
}