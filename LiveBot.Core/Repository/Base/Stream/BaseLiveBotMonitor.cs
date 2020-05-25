using LiveBot.Core.Repository.Enums;
using LiveBot.Core.Repository.Interfaces.Stream;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LiveBot.Core.Repository.Base.Stream
{
    /// <summary>
    /// Represents a Base implementation of <c>ILiveBotMonitor</c>
    /// </summary>
    public abstract class BaseLiveBotMonitor : ILiveBotMonitor
    {
        protected BaseLiveBotMonitor()
        {
        }

        public string URLPattern { get; set; }
        public string BaseURL { get; set; }
        public ServiceEnum ServiceType { get; set; }

        public abstract ILiveBotMonitorStart GetStartClass();

        public abstract Task<ILiveBotGame> GetGame(string gameId);

        public abstract Task<ILiveBotStream> GetStream(ILiveBotUser user);

        public abstract Task<ILiveBotUser> GetUser(string username = null, string userId = null);

        public bool IsValid(string streamURL)
        {
            return Regex.IsMatch(streamURL, URLPattern);
        }
    }
}