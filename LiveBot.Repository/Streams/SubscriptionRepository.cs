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

        /// <inheritdoc/>
        public Task<List<StreamSubscription>> GetForUserWithRolesAsync(StreamUser user) =>
            DbSet
                .Where(s => s.Deleted == false)
                .Where(s => s.User == user)
                .Include(s => s.User)
                .Include(s => s.DiscordGuild)
                .Include(s => s.DiscordChannel)
                .Include(s => s.RolesToMention)
                .AsSplitQuery()
                .ToListAsync();

        /// <inheritdoc/>
        public async Task RemoveWithRolesAsync(long subscriptionId)
        {
            await using (var transaction = await Context.Database.BeginTransactionAsync().ConfigureAwait(false))
            {
                await Context.Set<RoleToMention>().Where(r => r.StreamSubscription.Id == subscriptionId).ExecuteDeleteAsync().ConfigureAwait(false);
                await DbSet.Where(s => s.Id == subscriptionId).ExecuteDeleteAsync().ConfigureAwait(false);
                await transaction.CommitAsync().ConfigureAwait(false);
            }

            // A stale tracked row would be written again by a later save (FK nulled or row deleted) and fail on zero rows affected.
            // The foreign key shadow property is read because touching the navigation would lazy load it.
            var staleRoles = Context.ChangeTracker.Entries<RoleToMention>()
                .Where(e => e.Property<long?>("StreamSubscriptionId").CurrentValue == subscriptionId)
                .ToList();
            foreach (var entry in staleRoles)
                entry.State = EntityState.Detached;

            var staleSubscriptions = Context.ChangeTracker.Entries<StreamSubscription>()
                .Where(e => e.Entity.Id == subscriptionId)
                .ToList();
            foreach (var entry in staleSubscriptions)
                entry.State = EntityState.Detached;
        }
    }
}