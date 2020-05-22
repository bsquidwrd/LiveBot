namespace LiveBot.Core.Repository.Interfaces.Stream
{
    public interface ILiveBotUser : ILiveBotBase
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Displayname { get; set; }
        public string BaseURL { get; set; }
        public string BroadcasterType { get; set; }
        public string AvatarURL { get; set; }

        public string GetProfileURL();
    }
}