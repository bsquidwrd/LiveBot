using LiveBot.Core.Repository.Base.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

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
            AvatarURL = user.ProfileImageUrl;
            ProfileURL = $"{BaseURL}/{Username}";
        }

        public TwitchUser(string baseURL, ServiceEnum serviceType, StreamUser user) : base(baseURL, serviceType)
        {
            Id = user.SourceID;
            Username = user.Username;
            DisplayName = user.DisplayName;
            AvatarURL = user.AvatarURL;
            ProfileURL = user.ProfileURL;
        }

        public TwitchUser()
        { }
    }
}