using LiveBot.Core.Repository.Enums;
using LiveBot.Core.Repository.Interfaces.Monitor;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LiveBot.Core.Repository.Base.Monitor
{
    /// <summary>
    /// Represents a Base implementation of <c>ILiveBotMonitor</c>
    /// </summary>
    public abstract class BaseLiveBotMonitor : ILiveBotMonitor
    {
        protected BaseLiveBotMonitor()
        {
        }

        public string ServiceName { get; set; }
        public string URLPattern { get; set; }
        public string BaseURL { get; set; }
        public ServiceEnum ServiceType { get; set; }

        public Regex GetURLRegex(string pattern)
        {
            return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public abstract ILiveBotMonitorStart GetStartClass();

        public abstract Task<ILiveBotGame> GetGame(string gameId);

        public abstract Task<ILiveBotStream> GetStream(ILiveBotUser user);

        public abstract Task<ILiveBotUser> GetUser(string username = null, string userId = null, string profileURL = null);

        public bool IsValid(string streamURL)
        {
            return Regex.IsMatch(streamURL, URLPattern);
        }
    }
}