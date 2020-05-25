using LiveBot.Core.Repository.Enums;
using LiveBot.Core.Repository.Interfaces.Stream;

namespace LiveBot.Core.Repository.Base.Stream
{
    /// <summary>
    /// Base implementation of <c>ILiveBotUser</c>
    /// </summary>
    public abstract class BaseLiveBotUser : BaseLiveBot, ILiveBotUser
    {
        public BaseLiveBotUser(string baseURL, ServiceEnum serviceType) : base(baseURL, serviceType)
        {
        }

        public string Id { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string BroadcasterType { get; set; }
        public string AvatarURL { get; set; }

        public abstract string GetProfileURL();

        public override string ToString()
        {
            return $"{ServiceType.ToString()}: {Username}";
        }
    }
}