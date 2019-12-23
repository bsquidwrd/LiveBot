﻿using LiveBot.Core.Repository;
using LiveBot.Core.Repository.Models;
using LiveBot.Repository.Models;
using System.Linq;

namespace LiveBot.Repository
{
    public class GuildRepository : IGuildRepository
    {
        private readonly LiveBotDBContext _context;

        public GuildRepository(LiveBotDBContext context)
        {
            this._context = context;
        }

        public IDiscordGuild GetGuild(ulong GuildID) => _context.DiscordGuild.Where(d => d.Id == GuildID).FirstOrDefault();

        public IDiscordGuild UpdateOrCreateGuild(ulong GuildID, string GuildName)
        {
            DiscordGuild discordGuild = null;
            try
            {
                discordGuild = _context.DiscordGuild.Where(d => d.Id == GuildID).FirstOrDefault();
                discordGuild.Name = GuildName;
            }
            catch
            {
                discordGuild = new DiscordGuild() { Id = GuildID, Name = GuildName };
                _context.DiscordGuild.Add(discordGuild);
            }
            finally
            {
                _context.SaveChanges();
            }
            return discordGuild;
        }
    }
}