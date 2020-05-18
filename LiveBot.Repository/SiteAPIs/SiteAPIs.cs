using LiveBot.Core.Repository.Interfaces.SiteAPIs;

namespace LiveBot.Repository.SiteAPIs
{
    internal class SiteAPIs : ISiteAPIs
    {
        public ITwitch Twitch { get; }

        public SiteAPIs()
        {
            Twitch = new Twitch();
        }
    }
}