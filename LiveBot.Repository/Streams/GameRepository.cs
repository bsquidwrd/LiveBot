using LiveBot.Core.Repository.Interfaces.Streams;
using LiveBot.Core.Repository.Models.Streams;

namespace LiveBot.Repository.Streams
{
    public class GameRepository : ModelRepository<StreamGame>, IGameRepository
    {
        public GameRepository(LiveBotDBContext context) : base(context)
        {
        }
    }
}