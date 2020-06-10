using LiveBot.Core.Repository.Base.Monitor;
using LiveBot.Core.Repository.Static;
using TwitchLib.Api.Helix.Models.Users;

namespace LiveBot.Watcher.Twitch.Models
{
    /// <summary>
    /// Represents a Twitch User
    /// </summary>
    public class TwitchUser : BaseLiveBotUser
    {
        public TwitchUser(string baseURL, ServiceEnum serviceType, User user) : base(baseURL, serviceType)
        {
            Id = user.Id;
            Username = user.Login;
            DisplayName = user.DisplayName;
            BroadcasterType = user.BroadcasterType;
            AvatarURL = user.ProfileImageUrl;
            ProfileURL = $"{BaseURL}/{Username}";
        }
    }
}