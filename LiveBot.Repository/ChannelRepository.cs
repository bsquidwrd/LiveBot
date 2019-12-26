using LiveBot.Core.Repository;
using LiveBot.Core.Repository.Models;

namespace LiveBot.Repository
{
    public class ChannelRepository : ModelRepository<DiscordChannel>, IChannelRepository
    {
        public ChannelRepository(LiveBotDBContext context) : base(context)
        {
        }
    }
}