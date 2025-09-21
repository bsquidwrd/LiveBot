using System.Collections.Generic;
using LiveBot.Core.Repository.Interfaces.Monitor;

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
        string ServiceType { get; set; }

        /// <summary>
        /// The streams to check for catch-up
        /// </summary>
        IEnumerable<ILiveBotUser> StreamUsers { get; set; }
    }
}