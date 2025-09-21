using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace LiveBot.Core.Interfaces.Discord
{
    public interface IDiscordStats : IHostedService
    {
        /// <summary>
        /// Used to update stats
        /// </summary>
        /// <returns></returns>
        public Task UpdateStats();
    }
}