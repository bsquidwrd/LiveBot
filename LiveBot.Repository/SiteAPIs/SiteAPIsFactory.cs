using LiveBot.Core.Repository.Interfaces.SiteAPIs;

namespace LiveBot.Repository.SiteAPIs
{
    public class SiteAPIsFactory : ISiteAPIsFactory
    {
        public SiteAPIsFactory()
        {
        }

        public ISiteAPIs Create()
        {
            return new SiteAPIs();
        }
    }
}