using LiveBot.Core.Repository.Interfaces.SiteAPIs;

namespace LiveBot.Repository.SiteAPIs
{
    internal class SiteAPIs : ISiteAPIs
    {
        public ITwitchAPI TwitchAPI { get; }

        public SiteAPIs()
        {
            TwitchAPI = new TwitchAPI();
        }
    }
}