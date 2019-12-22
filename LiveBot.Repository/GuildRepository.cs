using LiveBot.Core.Repository;

namespace LiveBot.Repository
{
    public class GuildRepository : IGuildRepository
    {
        public ulong GetGuild(ulong GuildID)
        {
            return GuildID;
        }
    }
}