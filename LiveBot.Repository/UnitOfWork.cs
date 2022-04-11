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
        public ISubscriptionRepository SubscriptionRepository { get; }
        public IUserRepository UserRepository { get; }
        public INotificationRepository NotificationRepository { get; }
        public IGameRepository GameRepository { get; }
        public IAuthRepository AuthRepository { get; }
        public IGuildConfigRepository GuildConfigRepository { get; }
        public IRoleToMentionRepository RoleToMentionRepository { get; }

        public UnitOfWork(LiveBotDBContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            GuildRepository = new GuildRepository(context);
            ChannelRepository = new ChannelRepository(context);
            SubscriptionRepository = new SubscriptionRepository(context);
            UserRepository = new UserRepository(context);
            NotificationRepository = new NotificationRepository(context);
            GameRepository = new GameRepository(context);
            AuthRepository = new AuthRepository(context);
            GuildConfigRepository = new GuildConfigRepository(context);
            RoleToMentionRepository = new RoleToMentionRepository(context);
        }

        public LiveBotDBContext GetContext() =>
            _context;
    }
}