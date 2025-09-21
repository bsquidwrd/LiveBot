using System.Collections.Generic;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Static;

namespace LiveBot.Core.Contracts
{
    /// <summary>
    /// Contract for startup catch-up operations
    /// </summary>
    public interface IStartupCatchup
    {
        /// <summary>
        /// The service type for the catch-up
        /// </summary>
        ServiceEnum ServiceType { get; set; }

        /// <summary>
        /// The streams to check for catch-up
        /// </summary>
        IEnumerable<ILiveBotUser> StreamUsers { get; set; }
    }
}