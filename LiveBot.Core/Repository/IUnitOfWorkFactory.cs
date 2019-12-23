namespace LiveBot.Core.Repository
{
    public interface IUnitOfWorkFactory
    {
        IUnitOfWork Create();
    }
}