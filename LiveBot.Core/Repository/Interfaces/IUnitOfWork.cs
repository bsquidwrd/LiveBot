using System;
using LiveBot.Core.Repository.Interfaces.Discord;
using LiveBot.Core.Repository.Interfaces.Streams;

namespace LiveBot.Core.Repository.Interfaces
{
    /// <summary>
    /// Represents an interactable Database Instance. Use one per logical operation and dispose it when
    /// the operation ends; entities it returns cannot lazy-load after that.
    /// </summary>
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        IGuildRepository GuildRepository { get; }
        IChannelRepository ChannelRepository { get; }
        ISubscriptionRepository SubscriptionRepository { get; }
        IUserRepository UserRepository { get; }
        INotificationRepository NotificationRepository { get; }
        IGameRepository GameRepository { get; }
        IAuthRepository AuthRepository { get; }
        IGuildConfigRepository GuildConfigRepository { get; }
        IRoleToMentionRepository RoleToMentionRepository { get; }
    }
}