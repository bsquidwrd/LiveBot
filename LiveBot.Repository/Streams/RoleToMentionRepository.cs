using LiveBot.Core.Repository.Interfaces.Streams;
using LiveBot.Core.Repository.Models.Streams;

namespace LiveBot.Repository.Streams
{
    /// <inheritdoc/>
    public class RoleToMentionRepository : ModelRepository<RoleToMention>, IRoleToMentionRepository
    {
        /// <inheritdoc/>
        public RoleToMentionRepository(LiveBotDBContext context) : base(context)
        {
        }
    }
}