using LiveBot.Core.Repository.Base.Stream;
using LiveBot.Core.Repository.Enums;
using TwitchLib.Api.Helix.Models.Users;

namespace LiveBot.Watcher.Twitch.Models
{
    public class TwitchUser : BaseLiveBotUser
    {
        public TwitchUser(string baseURL, ServiceEnum serviceType, User user) : base(baseURL, serviceType)
        {
            Id = user.Id;
            Username = user.Login;
            Displayname = user.DisplayName;
            BroadcasterType = user.BroadcasterType;
            AvatarURL = user.ProfileImageUrl;
        }

        public override string GetProfileURL()
        {
            return $@"{BaseURL}/{Username}";
        }
    }
}