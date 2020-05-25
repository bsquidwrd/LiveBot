using LiveBot.Core.Repository.Interfaces.Discord;
using LiveBot.Core.Repository.Models.Discord;

namespace LiveBot.Repository.Discord
{
    /// <inheritdoc/>
    public class GuildRepository : ModelRepository<DiscordGuild>, IGuildRepository
    {
        /// <inheritdoc/>
        public GuildRepository(LiveBotDBContext context) : base(context)
        {
        }
    }
}