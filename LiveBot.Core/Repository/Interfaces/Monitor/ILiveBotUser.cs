namespace LiveBot.Core.Repository.Interfaces.Monitor
{
    /// <summary>
    /// Represents a generic User for use within the bot, usually returned by a Monitoring Service
    /// </summary>
    public interface ILiveBotUser : ILiveBotBase
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string AvatarURL { get; set; }
        public string ProfileURL { get; set; }
    }
}