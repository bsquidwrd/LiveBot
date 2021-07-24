using LiveBot.Core.Repository.Models;
using LiveBot.Core.Repository.Static;
using TwitchLib.Api.Auth;

namespace LiveBot.Watcher.Twitch.Models
{
    public class TwitchAuth : MonitorAuth
    {
        public TwitchAuth(ServiceEnum serviceType, string clientId, RefreshResponse refreshResponse)
        {
            ServiceType = serviceType;
            Expired = false;
            ClientId = clientId;
            AccessToken = refreshResponse.AccessToken;
            RefreshToken = refreshResponse.RefreshToken;
        }
    }
}