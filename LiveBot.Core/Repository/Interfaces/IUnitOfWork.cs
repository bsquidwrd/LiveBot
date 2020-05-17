namespace LiveBot.Core.Repository.Interfaces
{
    public interface IUnitOfWork
    {
        //IExampleRepository ExampleRepository { get; }
        IGuildRepository GuildRepository { get; }

        IChannelRepository ChannelRepository { get; }
    }
}