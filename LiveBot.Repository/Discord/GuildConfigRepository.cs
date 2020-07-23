using LiveBot.Core.Repository.Interfaces.Discord;
using LiveBot.Core.Repository.Models.Discord;

namespace LiveBot.Repository.Discord
{
    /// <inheritdoc/>
    public class GuildConfigRepository : ModelRepository<DiscordGuildConfig>, IGuildConfigRepository
    {
        /// <inheritdoc/>
        public GuildConfigRepository(LiveBotDBContext context) : base(context)
        {
        }
    }
}