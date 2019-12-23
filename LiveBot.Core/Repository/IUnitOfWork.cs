namespace LiveBot.Core.Repository
{
    public interface IUnitOfWork
    {
        ILiveBotDBContext _context { get; }
        IExampleRepository ExampleRepository { get; }
        IGuildRepository GuildRepository { get; }

        public void Create();
    }
}