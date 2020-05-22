using LiveBot.Core.Repository.Enums;
using LiveBot.Core.Repository.Interfaces.Stream;

namespace LiveBot.Core.Repository.Base.Stream
{
    public abstract class BaseLiveBotUser : BaseLiveBot, ILiveBotUser
    {
        public BaseLiveBotUser(string baseURL, ServiceEnum serviceType) : base(baseURL, serviceType)
        {
        }

        public string Id { get; set; }
        public string Username { get; set; }
        public string Displayname { get; set; }
        public string BroadcasterType { get; set; }
        public string AvatarURL { get; set; }

        public abstract string GetProfileURL();

        public override string ToString()
        {
            return $@"{ServiceType.ToString()}: {Username}";
        }
    }
}