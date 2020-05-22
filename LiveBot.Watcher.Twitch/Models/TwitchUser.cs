using LiveBot.Core.Repository.Interfaces.Stream;
using TwitchLib.Api.Helix.Models.Users;

namespace LiveBot.Watcher.Twitch.Models
{
    public class TwitchUser : BaseLiveBotUser
    {
        public TwitchUser(User user)
        {
            ServiceName = "Twitch";
            BaseURL = "https://twitch.tv";
            Id = user.Id;
            Username = user.Login;
        }
        public override string GetStreamURL()
        {
            return $@"{BaseURL}/{Username}";
        }
    }
}