using LiveBot.Core.Repository;
using LiveBot.Core.Repository.Models;
using LiveBot.Repository.Models;

namespace LiveBot.Repository
{
    public class GuildRepository : IGuildRepository
    {
        private readonly ILiveBotDBContext _context;

        public GuildRepository(ILiveBotDBContext context)
        {
            this._context = context;
        }

        public IDiscordGuild GetGuild(ulong GuildID)
        {
            return _context.DiscordGuild.where(d => d.Id == GuildID);
        }

        public IDiscordGuild UpdateOrCreateGuild(ulong GuildID, string GuildName)
        {
            IDiscordGuild discordGuild = null;
            try
            {
                discordGuild = _context.DiscordGuild.where(discordGuild => d.Id == GuildID);
                discordGuild.Name = GuildName;
                discordGuild.SaveChanges();
            }
            catch
            {
                discordGuild = new DiscordGuild() { Id = GuildID, Name = GuildName };
            }
            return discordGuild;
        }
    }
}