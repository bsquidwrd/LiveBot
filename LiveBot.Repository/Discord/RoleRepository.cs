using LiveBot.Core.Repository.Interfaces.Discord;
using LiveBot.Core.Repository.Models.Discord;

namespace LiveBot.Repository.Discord
{
    /// <inheritdoc/>
    public class RoleRepository : ModelRepository<DiscordRole>, IRoleRepository
    {
        /// <inheritdoc/>
        public RoleRepository(LiveBotDBContext context) : base(context)
        {
        }
    }
}