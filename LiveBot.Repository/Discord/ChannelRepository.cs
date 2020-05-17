using LiveBot.Core.Repository.Interfaces.Discord;
using LiveBot.Core.Repository.Models.Discord;

namespace LiveBot.Repository.Discord
{
    public class ChannelRepository : ModelRepository<DiscordChannel>, IChannelRepository
    {
        public ChannelRepository(LiveBotDBContext context) : base(context)
        {
        }
    }
}