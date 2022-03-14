using Microsoft.Extensions.Hosting;
using System.Timers;

namespace LiveBot.Core.Repository.Interfaces.Discord
{
    public interface IDiscordStats : IHostedService
    {
        /// <summary>
        /// Used to update stats
        /// </summary>
        /// <returns></returns>
        public void UpdateStats(object sender = null, ElapsedEventArgs e = null);
    }
}