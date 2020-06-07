﻿using LiveBot.Core.Repository.Interfaces.Discord;
using LiveBot.Core.Repository.Interfaces.Streams;

namespace LiveBot.Core.Repository.Interfaces
{
    /// <summary>
    /// Represents an interactable Database Instance
    /// </summary>
    public interface IUnitOfWork
    {
        IGuildRepository GuildRepository { get; }
        IChannelRepository ChannelRepository { get; }
        IRoleRepository RoleRepository { get; }
        IStreamSubscriptionRepository StreamSubscriptionRepository { get; }
    }
}