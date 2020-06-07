using LiveBot.Core.Repository.Interfaces.Streams;
using LiveBot.Core.Repository.Models.Streams;

namespace LiveBot.Repository.Streams
{
    /// <inheritdoc/>
    public class StreamSubscriptionRepository : ModelRepository<StreamSubscription>, IStreamSubscriptionRepository
    {
        /// <inheritdoc/>
        public StreamSubscriptionRepository(LiveBotDBContext context) : base(context)
        {
        }
    }
}