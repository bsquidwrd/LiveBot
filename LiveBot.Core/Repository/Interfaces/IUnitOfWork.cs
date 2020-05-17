using LiveBot.Core.Repository.Interfaces.Discord;

namespace LiveBot.Core.Repository.Interfaces
{
    public interface IUnitOfWork
    {
        IGuildRepository GuildRepository { get; }

        IChannelRepository ChannelRepository { get; }
    }
}