using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiveBot.Core.Repository.Interfaces.Streams;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;
using Microsoft.EntityFrameworkCore;

namespace LiveBot.Repository.Streams
{
    /// <inheritdoc/>
    public class SubscriptionRepository : ModelRepository<StreamSubscription>, ISubscriptionRepository
    {
        /// <inheritdoc/>
        public SubscriptionRepository(LiveBotDBContext context) : base(context)
        {
        }

        /// <inheritdoc/>
        public Task<List<string>> GetMonitoredSourceIdsAsync(ServiceEnum serviceType) =>
            DbSet
                .AsNoTracking()
                .Where(s => s.Deleted == false && s.User.ServiceType == serviceType)
                .Select(s => s.User.SourceID)
                .Distinct()
                .ToListAsync();
    }
}