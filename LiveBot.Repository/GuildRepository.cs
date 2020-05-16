using LiveBot.Core.Repository;
using LiveBot.Core.Repository.Models.Discord;

namespace LiveBot.Repository
{
    public class GuildRepository : ModelRepository<DiscordGuild>, IGuildRepository
    {
        public GuildRepository(LiveBotDBContext context) : base(context)
        {
        }
    }
}