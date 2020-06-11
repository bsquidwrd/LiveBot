using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiveBot.Repository
{
    public class AuthRepository : ModelRepository<MonitorAuth>, IAuthRepository
    {
        public AuthRepository(LiveBotDBContext context) : base(context)
        {
        }
    }
}
