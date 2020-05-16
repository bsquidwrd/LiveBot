namespace LiveBot.Core.Repository.Models.Twitch
{
    public class TwitchChannel : BaseTwitchModel<TwitchChannel>
    {
        public string DisplayName { get; set; }
        public string ProfileImage { get; set; }
        public string OfflineImage { get; set; }
    }
}