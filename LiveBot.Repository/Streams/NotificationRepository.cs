using LiveBot.Core.Repository.Interfaces.Streams;
using LiveBot.Core.Repository.Models.Streams;

namespace LiveBot.Repository.Streams
{
    public class NotificationRepository : ModelRepository<StreamNotification>, INotificationRepository
    {
        /// <inheritdoc/>
        public NotificationRepository(LiveBotDBContext context) : base(context)
        {
        }
    }
}