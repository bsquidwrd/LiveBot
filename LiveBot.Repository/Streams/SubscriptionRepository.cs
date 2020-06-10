using LiveBot.Core.Repository.Interfaces.Streams;
using LiveBot.Core.Repository.Models.Streams;

namespace LiveBot.Repository.Streams
{
    /// <inheritdoc/>
    public class SubscriptionRepository : ModelRepository<StreamSubscription>, ISubscriptionRepository
    {
        /// <inheritdoc/>
        public SubscriptionRepository(LiveBotDBContext context) : base(context)
        {
        }
    }
}