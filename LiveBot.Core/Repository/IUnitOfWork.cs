namespace LiveBot.Core.Repository
{
    public interface IUnitOfWork
    {
        IExampleRepository ExampleRepository { get; }
        IGuildRepository GuildRepository { get; }
        IChannelRepository ChannelRepository { get; }
    }
}