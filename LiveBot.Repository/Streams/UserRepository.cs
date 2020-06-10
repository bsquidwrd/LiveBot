using LiveBot.Core.Repository.Interfaces.Streams;
using LiveBot.Core.Repository.Models.Streams;

namespace LiveBot.Repository.Streams
{
    /// <inheritdoc/>
    public class UserRepository : ModelRepository<StreamUser>, IUserRepository
    {
        /// <inheritdoc/>
        public UserRepository(LiveBotDBContext context) : base(context)
        {
        }
    }
}