namespace LiveBot.Core.Repository.Interfaces.Monitor
{
    /// <summary>
    /// Represents a generic Game for use within the bot, usually returned by a Monitoring Service
    /// </summary>
    public interface ILiveBotGame : ILiveBotBase
    {
        public string Id { get; }
        public string Name { get; }
        public string ThumbnailURL { get; }
    }
}