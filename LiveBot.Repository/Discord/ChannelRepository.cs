using LiveBot.Core.Repository.Interfaces.Discord;
using LiveBot.Core.Repository.Models.Discord;

namespace LiveBot.Repository.Discord
{
    /// <inheritdoc/>
    public class ChannelRepository : ModelRepository<DiscordChannel>, IChannelRepository
    {
        /// <inheritdoc/>
        public ChannelRepository(LiveBotDBContext context) : base(context)
        {
        }
    }
}