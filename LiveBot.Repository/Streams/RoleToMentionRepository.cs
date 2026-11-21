using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiveBot.Core.Repository.Interfaces.Streams;
using LiveBot.Core.Repository.Models.Streams;
using Microsoft.EntityFrameworkCore;

namespace LiveBot.Repository.Streams
{
    /// <inheritdoc/>
    public class RoleToMentionRepository : ModelRepository<RoleToMention>, IRoleToMentionRepository
    {
        /// <inheritdoc/>
        public RoleToMentionRepository(LiveBotDBContext context) : base(context)
        {
        }

        /// <inheritdoc/>
        public async Task RemoveRangeAsync(IReadOnlyCollection<long> ids)
        {
            if (ids.Count == 0)
                return;

            await DbSet.Where(r => ids.Contains(r.Id)).ExecuteDeleteAsync().ConfigureAwait(false);

            // A stale tracked row would be written again by a later save (FK nulled or row deleted) and fail on zero rows affected.
            foreach (var entry in Context.ChangeTracker.Entries<RoleToMention>().Where(e => ids.Contains(e.Entity.Id)).ToList())
                entry.State = EntityState.Detached;
        }
    }
}