using System.Collections.Generic;
using System.Threading.Tasks;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;

namespace LiveBot.Core.Repository.Interfaces.Streams
{
    /// <summary>
    /// Interface for Database interaction with a Stream User
    /// </summary>
    public interface IUserRepository : IRepository<StreamUser>
    {
        /// <summary>
        /// Copies the profile fields of <paramref name="profiles"/> onto the matching stored users of
        /// <paramref name="serviceType"/> with one read and one save. Profiles without a stored user are skipped.
        /// </summary>
        Task UpdateProfilesAsync(ServiceEnum serviceType, IReadOnlyCollection<ILiveBotUser> profiles);
    }
}