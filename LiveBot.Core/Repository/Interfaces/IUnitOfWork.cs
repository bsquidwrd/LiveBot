using LiveBot.Core.Repository.Interfaces.Discord;

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
    }
}