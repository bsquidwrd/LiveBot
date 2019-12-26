using LiveBot.Core.Repository;
using System;

namespace LiveBot.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly LiveBotDBContext _context;
        public IExampleRepository ExampleRepository { get; }
        public IGuildRepository GuildRepository { get; }
        public IChannelRepository ChannelRepository { get; }

        public UnitOfWork(LiveBotDBContext context)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            ExampleRepository = new ExampleRepository();
            GuildRepository = new GuildRepository(context);
            ChannelRepository = new ChannelRepository(context);
        }
    }
}