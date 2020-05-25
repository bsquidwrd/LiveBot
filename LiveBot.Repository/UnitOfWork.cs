using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Discord;
using LiveBot.Repository.Discord;
using System;

namespace LiveBot.Repository
{
    /// <inheritdoc/>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly LiveBotDBContext _context;
        public IGuildRepository GuildRepository { get; }

        public IChannelRepository ChannelRepository { get; }

        public UnitOfWork(LiveBotDBContext context)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            //ExampleRepository = new ExampleRepository();
            GuildRepository = new GuildRepository(context);
            ChannelRepository = new ChannelRepository(context);
        }
    }
}