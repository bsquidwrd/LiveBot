using LiveBot.Core.Repository.Interfaces;
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