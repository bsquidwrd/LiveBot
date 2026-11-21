using System.Collections.Generic;
using System.Threading.Tasks;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;

namespace LiveBot.Core.Repository.Interfaces.Streams
{
    /// <summary>
    /// Interface for Database interaction with a Stream Subscription
    /// </summary>
    public interface ISubscriptionRepository : IRepository<StreamSubscription>
    {
        /// <summary>
        /// Gets the distinct source ids of every user with an active subscription for the service,
        /// without loading the subscription graph.
        /// </summary>
        /// <param name="serviceType">The service to get monitored users for.</param>
        /// <returns>The distinct source ids.</returns>
        Task<List<string>> GetMonitoredSourceIdsAsync(ServiceEnum serviceType);
    }
}
