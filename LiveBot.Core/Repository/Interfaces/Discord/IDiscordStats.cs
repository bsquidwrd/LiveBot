using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace LiveBot.Core.Repository.Interfaces.Discord
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