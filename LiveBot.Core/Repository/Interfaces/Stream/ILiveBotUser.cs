namespace LiveBot.Core.Repository.Interfaces.Stream
{
    public interface ILiveBotUser
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string ServiceName { get; set; }
        public string BaseURL { get; set; }

        public string GetStreamURL();
    }
}