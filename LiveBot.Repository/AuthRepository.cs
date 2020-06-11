using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models;

namespace LiveBot.Repository
{
    public class AuthRepository : ModelRepository<MonitorAuth>, IAuthRepository
    {
        public AuthRepository(LiveBotDBContext context) : base(context)
        {
        }
    }
}