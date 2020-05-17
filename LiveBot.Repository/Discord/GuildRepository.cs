using LiveBot.Core.Repository.Interfaces.Discord;
using LiveBot.Core.Repository.Models.Discord;

namespace LiveBot.Repository.Discord
{
    public class GuildRepository : ModelRepository<DiscordGuild>, IGuildRepository
    {
        public GuildRepository(LiveBotDBContext context) : base(context)
        {
        }
    }
}