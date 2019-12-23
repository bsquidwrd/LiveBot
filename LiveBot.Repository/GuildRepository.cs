using LiveBot.Core.Repository;
using LiveBot.Core.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LiveBot.Repository
{
    public class GuildRepository : ModelRepository<DiscordGuild>, IGuildRepository
    {
        public GuildRepository(LiveBotDBContext context) : base(context)
        {
        }
    }
}