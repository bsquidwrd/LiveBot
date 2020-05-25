namespace LiveBot.Core.Repository.Interfaces
{
    /// <summary>
    /// Factory wrapper for a Database Factory
    /// </summary>
    public interface IUnitOfWorkFactory
    {
        /// <summary>
        /// Used to create an instance of the Database
        /// </summary>
        /// <returns></returns>
        IUnitOfWork Create();
    }
}