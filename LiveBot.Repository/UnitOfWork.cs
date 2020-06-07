using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Discord;
using LiveBot.Core.Repository.Interfaces.Streams;
using LiveBot.Repository.Discord;
using LiveBot.Repository.Streams;
using System;

namespace LiveBot.Repository
{
    /// <inheritdoc/>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly LiveBotDBContext _context;
        public IGuildRepository GuildRepository { get; }
        public IChannelRepository ChannelRepository { get; }
        public IRoleRepository RoleRepository { get; }
        public IStreamSubscriptionRepository StreamSubscriptionRepository { get; }

        public UnitOfWork(LiveBotDBContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            GuildRepository = new GuildRepository(context);
            ChannelRepository = new ChannelRepository(context);
            RoleRepository = new RoleRepository(context);
            StreamSubscriptionRepository = new StreamSubscriptionRepository(context);
        }
    }
}