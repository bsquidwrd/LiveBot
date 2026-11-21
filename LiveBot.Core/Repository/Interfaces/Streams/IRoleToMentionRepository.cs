using System.Collections.Generic;
using System.Threading.Tasks;
using LiveBot.Core.Repository.Models.Streams;

namespace LiveBot.Core.Repository.Interfaces.Streams
{
    public interface IRoleToMentionRepository : IRepository<RoleToMention>
    {
        /// <summary>
        /// Permanently deletes the given roles to mention in one query. Instances already loaded in the
        /// context are detached.
        /// </summary>
        /// <param name="ids">The ids of the roles to mention to delete.</param>
        Task RemoveRangeAsync(IReadOnlyCollection<long> ids);
    }
}