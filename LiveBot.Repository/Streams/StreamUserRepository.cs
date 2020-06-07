using LiveBot.Core.Repository.Interfaces.Streams;
using LiveBot.Core.Repository.Models.Streams;

namespace LiveBot.Repository.Streams
{
    /// <inheritdoc/>
    public class StreamUserRepository : ModelRepository<StreamUser>, IStreamUserRepository
    {
        /// <inheritdoc/>
        public StreamUserRepository(LiveBotDBContext context) : base(context)
        {
        }
    }
}