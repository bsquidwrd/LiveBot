using System;
using System.Threading.Tasks;

namespace LiveBot.Core.Repository.Interfaces.Stream
{
    public interface ILiveBotMonitorStart
    {
        public Task StartAsync(IServiceProvider services);
    }
}