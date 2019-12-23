using LiveBot.Core.Repository.Models;

namespace LiveBot.Core.Repository
{
    public interface IGuildRepository
    {
        IDiscordGuild GetGuildAsync(ulong GuildID);

        IDiscordGuild UpdateOrCreateGuild(ulong GuildID, string GuildName);
    }
}