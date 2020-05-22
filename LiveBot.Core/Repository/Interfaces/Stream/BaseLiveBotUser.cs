namespace LiveBot.Core.Repository.Interfaces.Stream
{
    public abstract class BaseLiveBotUser : ILiveBotUser
    {
        public BaseLiveBotUser()
        {
        }

        public string Id { get; set; }
        public string Username { get; set; }
        public string ServiceName { get; set; }
        public string BaseURL { get; set; }

        public abstract string GetStreamURL();

        public override string ToString()
        {
            return $@"{Username}";
        }
    }
}