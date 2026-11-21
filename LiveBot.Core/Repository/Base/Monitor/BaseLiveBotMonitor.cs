using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Static;

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

        public string URLPattern { get; set; }
        public string BaseURL { get; set; }
        public ServiceEnum ServiceType { get; set; }
        public IUnitOfWorkFactory _factory { get; set; }

        public IUnitOfWork _work
        {
            get => _factory.Create();
        }

        public DateTime StartTime { get; set; }
        public bool IsEnabled { get; set; }

        // URLPattern has a public setter, so the cache is validated against the pattern it was built from.
        private Regex _urlRegex;

        public Regex GetURLRegex(string pattern)
        {
            var cached = _urlRegex;
            if (cached != null && cached.ToString() == pattern)
                return cached;

            var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            if (pattern == URLPattern)
                _urlRegex = regex;

            return regex;
        }

        public abstract Task StartAsync(bool IsWatcher = false);

        public abstract Task<ILiveBotGame> GetGame(string gameId);

        public abstract Task<ILiveBotStream> GetStream(ILiveBotUser user);

        public abstract Task<ILiveBotStream> GetStream(string userId);

        public abstract Task<ILiveBotStream> GetStream_Force(ILiveBotUser user);

        public abstract Task<ILiveBotUser> GetUserById(string userId);

        public abstract Task<ILiveBotUser> GetUser(string username = null, string userId = null, string profileURL = null);

        public bool IsValid(string streamURL)
        {
            return Regex.IsMatch(streamURL, URLPattern);
        }

        public abstract bool AddChannel(ILiveBotUser user);

        public abstract bool RemoveChannel(ILiveBotUser user);
    }
}