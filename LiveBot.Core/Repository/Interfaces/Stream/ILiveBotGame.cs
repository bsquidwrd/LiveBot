namespace LiveBot.Core.Repository.Interfaces.Stream
{
    public interface ILiveBotGame : ILiveBotBase
    {
        public string Id { get; }
        public string Name { get; }
        public string ThumbnailURL { get; }
    }
}