using System;
using System.Threading.Tasks;

namespace LiveBot.Core.Repository.Interfaces.Monitor
{
    /// <summary>
    /// Generic interface for a start class within a Monitoring Service
    /// </summary>
    public interface ILiveBotMonitorStart
    {
        /// <summary>
        /// Starts the Monitoring Service
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public Task StartAsync(ILiveBotMonitor monitor, IServiceProvider services);
    }
}