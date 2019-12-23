using LiveBot.Core.Repository;
using System;

namespace LiveBot.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        public ILiveBotDBContext _context { get; }
        public IExampleRepository ExampleRepository { get; }
        public IGuildRepository GuildRepository { get; }

        public UnitOfWork(LiveBotDBContext context)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            ExampleRepository = new ExampleRepository();
            GuildRepository = new GuildRepository(context);
        }

        public void Create()
        {
            throw new NotImplementedException();
        }
    }
}