using LiveBot.Core.Repository.Base.Monitor;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;
using LiveBot.Watcher.Twitch.Contracts;
using LiveBot.Watcher.Twitch.Models;
using MassTransit;
using System.Collections.Concurrent;
using System.Timers;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Core.RateLimiter;
using TwitchLib.Api.Helix.Models.Games;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;

using TwitchLibStreams = TwitchLib.Api.Helix.Models.Streams.GetStreams;

namespace LiveBot.Watcher.Twitch
{
    public class TwitchMonitor : BaseLiveBotMonitor
    {
        private readonly ILogger<TwitchMonitor> _logger;
        public LiveStreamMonitorService Monitor;
        public TwitchAPI API;
        public IBusControl _bus;
        public bool IsWatcher = false;
        private readonly IConfiguration _configuration;
        private readonly int RetryDelay = 1000 * 5; // 5 seconds
        private readonly int ApiRetryCount = 5; // How many times to retry API requests

        public string ClientId
        {
            get => API.Settings.ClientId;
            set
            {
                API.Settings.ClientId = value;
            }
        }

        public string ClientSecret
        {
            get => API.Settings.Secret;
            set
            {
                API.Settings.Secret = value;
            }
        }

        public string AccessToken
        {
            get => API.Settings.AccessToken;
            set
            {
                API.Settings.AccessToken = value;
            }
        }

        public System.Timers.Timer RefreshAuthTimer;
        public System.Timers.Timer RefreshUsersTimer;
        public System.Timers.Timer ClearCacheTimer;

        // My caches
        private ConcurrentDictionary<string, ILiveBotGame> _gameCache = new ConcurrentDictionary<string, ILiveBotGame>();

        private ConcurrentDictionary<string, ILiveBotUser> _userCache = new ConcurrentDictionary<string, ILiveBotUser>();

        /// <summary>
        /// Represents the whole Service for Twitch Monitoring
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public TwitchMonitor(ILogger<TwitchMonitor> logger, IUnitOfWorkFactory factory, IBusControl bus, IConfiguration configuration)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            _logger = logger;
            _factory = factory;
            _bus = bus;
            _configuration = configuration;

            StartTime = DateTime.UtcNow;
            BaseURL = "https://twitch.tv";
            ServiceType = ServiceEnum.Twitch;
            URLPattern = "^((http|https):\\/\\/|)([\\w\\d]+\\.)?twitch\\.tv/(?<username>[a-zA-Z0-9_]{1,})";

            var rateLimiter = TimeLimiter.GetFromMaxCountByInterval(5000, TimeSpan.FromMinutes(1));
            API = new TwitchAPI(rateLimiter: rateLimiter);
            Monitor = new LiveStreamMonitorService(api: API, checkIntervalInSeconds: 60, maxStreamRequestCountPerRequest: 100);

            ClientId = _configuration.GetValue<string>("TwitchClientId");
            ClientSecret = _configuration.GetValue<string>("TwitchClientSecret");

            //Monitor.OnServiceTick += Monitor_OnServiceTick;
            Monitor.OnServiceStarted += Monitor_OnServiceStarted;
            Monitor.OnStreamOnline += Monitor_OnStreamOnline;
            Monitor.OnStreamOffline += Monitor_OnStreamOffline;
            Monitor.OnStreamUpdate += Monitor_OnStreamUpdate;
        }

        #region Events

        private void Monitor_OnServiceTick(object? sender, OnServiceTickArgs e)
        {
            _logger.LogDebug("Monitor_OnServiceTick was called");
        }

        public void Monitor_OnServiceStarted(object? sender, OnServiceStartedArgs e)
        {
            _logger.LogDebug("Monitor service successfully connected to Twitch!");
            RefreshUsersTimer.Start();
            ClearCacheTimer.Start();
        }

        public async void Monitor_OnStreamOnline(object? sender, OnStreamOnlineArgs e)
        {
            if (!IsWatcher) return;
            ILiveBotUser? user = await GetUserById(e.Stream.UserId);
            if (user == null) return;
            ILiveBotGame game = await GetGame(e.Stream.GameId);
            ILiveBotStream stream = new TwitchStream(BaseURL, ServiceType, e.Stream, user, game);
            await PublishStreamOnline(stream);
        }

        public async void Monitor_OnStreamUpdate(object? sender, OnStreamUpdateArgs e)
        {
            if (!IsWatcher) return;
            ILiveBotUser? user = await GetUserById(e.Stream.UserId);
            if (user == null) return;
            ILiveBotGame game = await GetGame(e.Stream.GameId);
            ILiveBotStream stream = new TwitchStream(BaseURL, ServiceType, e.Stream, user, game);
            await _PublishStreamUpdate(stream);
        }

        public async void Monitor_OnStreamOffline(object? sender, OnStreamOfflineArgs e)
        {
            if (!IsWatcher) return;
            ILiveBotUser? user = await GetUserById(e.Stream.UserId);
            if (user == null) return;
            ILiveBotGame game = await GetGame(e.Stream.GameId);
            ILiveBotStream stream = new TwitchStream(BaseURL, ServiceType, e.Stream, user, game);
            await _PublishStreamOffline(stream);
        }

        #endregion Events

        #region API Calls

        private async Task<Game?> API_GetGame(string gameId, int retryCount = 0)
        {
            try
            {
                List<string> gameIDs = new List<string> { gameId };
                GetGamesResponse games = await API.Helix.Games.GetGamesAsync(gameIds: gameIDs).ConfigureAwait(false);
                return games.Games.FirstOrDefault(i => i.Id == gameId);
            }
            catch (Exception e) when (e is BadGatewayException || e is InternalServerErrorException)
            {
                _logger.LogError($"{e}");
                if (retryCount <= ApiRetryCount)
                {
                    await Task.Delay(RetryDelay);
                    return await API_GetGame(gameId, retryCount + 1);
                }
                return null;
            }
            catch (Exception e) when (e is InvalidCredentialException || e is BadScopeException)
            {
                _logger.LogError($"{e}");
                await UpdateAuth();
                await Task.Delay(RetryDelay);
                return await API_GetGame(gameId);
            }
        }

        private async Task<User?> API_GetUserByLogin(string username, int retryCount = 0)
        {
            try
            {
                List<string> usernameList = new List<string> { username.ToLower() };
                GetUsersResponse apiUser = await API.Helix.Users.GetUsersAsync(logins: usernameList).ConfigureAwait(false);
                return apiUser.Users.FirstOrDefault(i => i.Login == username);
            }
            catch (Exception e) when (e is BadGatewayException || e is InternalServerErrorException)
            {
                _logger.LogError($"{e}");
                if (retryCount <= ApiRetryCount)
                {
                    await Task.Delay(RetryDelay);
                    return await API_GetUserByLogin(username, retryCount + 1);
                }
                return null;
            }
            catch (Exception e) when (e is InvalidCredentialException || e is BadScopeException)
            {
                _logger.LogError($"{e}");
                await UpdateAuth();
                await Task.Delay(RetryDelay);
                return await API_GetUserByLogin(username);
            }
        }

        private async Task<User?> API_GetUserById(string userId, int retryCount = 0)
        {
            try
            {
                List<string> userIdList = new List<string> { userId };
                GetUsersResponse apiUser = await API.Helix.Users.GetUsersAsync(ids: userIdList).ConfigureAwait(false);
                return apiUser.Users.FirstOrDefault(i => i.Id == userId);
            }
            catch (Exception e) when (e is BadGatewayException || e is InternalServerErrorException)
            {
                _logger.LogError($"{e}");
                if (retryCount <= ApiRetryCount)
                {
                    await Task.Delay(RetryDelay);
                    return await API_GetUserById(userId, retryCount + 1);
                }
                return null;
            }
            catch (Exception e) when (e is InvalidCredentialException || e is BadScopeException)
            {
                _logger.LogError($"{e}");
                await UpdateAuth();
                await Task.Delay(RetryDelay);
                return await API_GetUserById(userId);
            }
        }

        private async Task<GetUsersResponse?> API_GetUsersById(List<string> userIdList, int retryCount = 0)
        {
            try
            {
                GetUsersResponse apiUsers = await API.Helix.Users.GetUsersAsync(ids: userIdList).ConfigureAwait(false);
                return apiUsers;
            }
            catch (Exception e) when (e is BadGatewayException || e is InternalServerErrorException)
            {
                _logger.LogError($"{e}");
                if (retryCount <= ApiRetryCount)
                {
                    await Task.Delay(RetryDelay);
                    return await API_GetUsersById(userIdList, retryCount + 1);
                }
                return null;
            }
            catch (Exception e) when (e is InvalidCredentialException || e is BadScopeException)
            {
                _logger.LogError($"{e}");
                await UpdateAuth();
                await Task.Delay(RetryDelay);
                return await API_GetUsersById(userIdList);
            }
        }

        private async Task<User?> API_GetUserByURL(string url, int retryCount = 0)
        {
            try
            {
                string username = GetURLRegex(URLPattern).Match(url).Groups["username"].ToString().ToLower();
                return await API_GetUserByLogin(username: username);
            }
            catch (Exception e) when (e is BadGatewayException || e is InternalServerErrorException)
            {
                _logger.LogError($"{e}");
                if (retryCount <= ApiRetryCount)
                {
                    await Task.Delay(RetryDelay);
                    return await API_GetUserByURL(url, retryCount + 1);
                }
                return null;
            }
            catch (Exception e) when (e is InvalidCredentialException || e is BadScopeException)
            {
                _logger.LogError($"{e}");
                await UpdateAuth();
                await Task.Delay(RetryDelay);
                return await API_GetUserByURL(url);
            }
        }

        private async Task<TwitchLibStreams.Stream?> API_GetStream(ILiveBotUser user, int retryCount = 0)
        {
            try
            {
                List<string> userIds = new List<string>
                {
                    user.Id
                };
                var streams = await API.Helix.Streams.GetStreamsAsync(type: "live", userIds: userIds);
                return streams.Streams.Where(i => i.UserId == user.Id).FirstOrDefault();
            }
            catch (Exception e) when (e is BadGatewayException || e is InternalServerErrorException)
            {
                _logger.LogError($"{e}");
                if (retryCount <= ApiRetryCount)
                {
                    await Task.Delay(RetryDelay);
                    return await API_GetStream(user, retryCount + 1);
                }
                return null;
            }
            catch (Exception e) when (e is InvalidCredentialException || e is BadScopeException)
            {
                _logger.LogError($"{e}");
                await UpdateAuth();
                await Task.Delay(RetryDelay);
                return await API_GetStream(user);
            }
        }

        #endregion API Calls

        #region Misc Functions

        private static IEnumerable<List<T>> SplitList<T>(List<T> locations, int nSize = 30)
        {
            for (int i = 0; i < locations.Count; i += nSize)
            {
                yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
            }
        }

        public async Task SetActiveAuth()
        {
            var activeAuth = await _work.AuthRepository.SingleOrDefaultAsync(i => i.ServiceType == ServiceType && i.ClientId == ClientId && i.Expired == false);
            AccessToken = activeAuth.AccessToken;
        }

        public async Task UpdateAuth()
        {
            if (!IsWatcher)
            {
                try
                {
                    await SetActiveAuth();
                    _logger.LogDebug($"Set AccessToken to active auth");

                    // Trigger it 5 minutes before expiration time to be safe
                    SetupAuthTimer(TimeSpan.FromMinutes(5));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Unable to update AccessToken {ex}");
                }
            }
            else
            {
                _logger.LogDebug($"Refreshing Auth for {ServiceType}");
                await SetActiveAuth();
                var oldAuth = await _work.AuthRepository.SingleOrDefaultAsync(i => i.ServiceType == ServiceType && i.ClientId == ClientId && i.Expired == false);
                RefreshResponse refreshResponse = await API.Auth.RefreshAuthTokenAsync(refreshToken: oldAuth.RefreshToken, clientSecret: ClientSecret, clientId: ClientId);

                var newAuth = new TwitchAuth(ServiceType, ClientId, refreshResponse);
                await _work.AuthRepository.AddOrUpdateAsync(newAuth, i => i.ServiceType == ServiceType && i.ClientId == ClientId && i.AccessToken == newAuth.AccessToken);

                oldAuth.Expired = true;
                await _work.AuthRepository.AddOrUpdateAsync(oldAuth, i => i.ServiceType == ServiceType && i.ClientId == ClientId && i.AccessToken == oldAuth.AccessToken);
                AccessToken = newAuth.AccessToken;

                var ExpirationSeconds = refreshResponse.ExpiresIn < 1800 ? 1800 : refreshResponse.ExpiresIn;
                _logger.LogDebug($"Expiration time: {ExpirationSeconds}");

                TimeSpan refreshAuthTimeSpan = TimeSpan.FromSeconds(ExpirationSeconds);
                // Trigger it 5 minutes before expiration time to be safe
                SetupAuthTimer(refreshAuthTimeSpan.Subtract(TimeSpan.FromMinutes(5)));
            }
        }

        private void SetupAuthTimer(TimeSpan timeSpan)
        {
            if (RefreshAuthTimer != null)
                RefreshAuthTimer.Stop();

            RefreshAuthTimer = new System.Timers.Timer(timeSpan.TotalMilliseconds)
            {
                AutoReset = false
            };
            RefreshAuthTimer.Elapsed += async (sender, e) => await UpdateAuth();
            RefreshAuthTimer.Start();
        }

        public async Task UpdateUsers()
        {
            try
            {
                foreach (List<string> userIds in SplitList(Monitor.ChannelsToMonitor, nSize: 100))
                {
                    GetUsersResponse? users = await API_GetUsersById(userIds);
                    if (users == null) continue;
                    foreach (User user in users.Users)
                    {
                        TwitchUser twitchUser = new TwitchUser(BaseURL, ServiceType, user);
                        if (_userCache.ContainsKey(user.Id))
                        {
                            _userCache[user.Id] = twitchUser;
                        }
                        else
                        {
                            _userCache.TryAdd(user.Id, twitchUser);
                        }
                        try
                        {
                            StreamUser streamUser = await _work.UserRepository.SingleOrDefaultAsync(i => i.ServiceType == ServiceType && i.SourceID == user.Id);
                            streamUser.Username = twitchUser.Username;
                            streamUser.DisplayName = twitchUser.DisplayName;
                            streamUser.AvatarURL = twitchUser.AvatarURL;
                            streamUser.ProfileURL = twitchUser.ProfileURL;

                            await _work.UserRepository.AddOrUpdateAsync(streamUser, (i => i.ServiceType == streamUser.ServiceType && i.SourceID == streamUser.SourceID));
                        }
                        catch (Exception e)
                        {
                            _logger.LogError($"Error updating user {twitchUser.Username}: {e}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                SetupUserTimer(1);
                _logger.LogError($"Error updating users\n{e}");
            }
        }

        private System.Timers.Timer SetupUserTimer(double minutes = 5)
        {
            TimeSpan timeSpan = TimeSpan.FromMinutes(minutes);
            var timer = new System.Timers.Timer(timeSpan.TotalMilliseconds)
            {
                AutoReset = true
            };
            timer.Elapsed += async (sender, e) => await UpdateUsers();
            return timer;
        }

        public void ClearCache(object? sender, ElapsedEventArgs? e)
        {
            _userCache.Clear();
            _gameCache.Clear();
        }

        private System.Timers.Timer SetupCacheTimer()
        {
            TimeSpan timeSpan = TimeSpan.FromMinutes(15);
            var timer = new System.Timers.Timer(timeSpan.TotalMilliseconds)
            {
                AutoReset = true
            };
            timer.Elapsed += ClearCache;
            return timer;
        }

        #endregion Misc Functions

        #region Messaging Implementation

        public async Task PublishStreamOnline(ILiveBotStream stream)
        {
            try
            {
                await _bus.Publish(new TwitchStreamOnline { Stream = stream });
            }
            catch (Exception e)
            {
                _logger.LogError($"Error trying to publish StreamOnline:\n{e}");
            }
        }

        public async Task _PublishStreamUpdate(ILiveBotStream stream)
        {
            try
            {
                await _bus.Publish(new TwitchStreamUpdate { Stream = stream });
            }
            catch (Exception e)
            {
                _logger.LogError($"Error trying to publish StreamUpdate:\n{e}");
            }
        }

        public async Task _PublishStreamOffline(ILiveBotStream stream)
        {
            try
            {
                await _bus.Publish(new TwitchStreamOffline { Stream = stream });
            }
            catch (Exception e)
            {
                _logger.LogError($"Error trying to publish StreamOffline:\n{e}");
            }
        }

        #endregion Messaging Implementation

        #region Interface Requirements

        /// <inheritdoc/>
        public override async Task StartAsync(bool IsWatcher = false)
        {
            this.IsWatcher = IsWatcher;
            await UpdateAuth();
            if (IsWatcher)
            {
                var streamsubscriptions = await _work.SubscriptionRepository.FindAsync(i => i.User.ServiceType == ServiceType);
                List<string> channelList = streamsubscriptions.Select(i => i.User.SourceID).Distinct().ToList();

                if (channelList.Count() == 0)
                    // Add myself so startup doesn't fail if there's no users in the database
                    channelList.Add("22812120");

                Monitor.SetChannelsById(channelList);
                RefreshUsersTimer = SetupUserTimer();
                ClearCacheTimer = SetupCacheTimer();

                await Task.Run(Monitor.Start);
            }
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotGame> GetGame(string gameId)
        {
            if (_gameCache.ContainsKey(gameId))
                return _gameCache[gameId];
            Game? game = null;
            if (!string.IsNullOrWhiteSpace(gameId))
            {
                game = await API_GetGame(gameId);
            }
            var twitchGame = new TwitchGame(BaseURL, ServiceType, game);
            _gameCache.TryAdd(gameId, twitchGame);
            return twitchGame;
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotStream?> GetStream(ILiveBotUser user)
        {
            if (!Monitor.LiveStreams.ContainsKey(user.Id))
                return null;
            TwitchLibStreams.Stream stream = Monitor.LiveStreams[user.Id];
            ILiveBotGame game = await GetGame(stream.GameId);
            return new TwitchStream(BaseURL, ServiceType, stream, user, game);
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotStream?> GetStream(string userId)
        {
            if (!Monitor.LiveStreams.ContainsKey(userId))
                return null;
            TwitchLibStreams.Stream stream = Monitor.LiveStreams[userId];
            ILiveBotUser? user = await GetUser(userId: userId);
            ILiveBotGame? game = await GetGame(stream.GameId);
            if (user != null && game != null)
                return new TwitchStream(BaseURL, ServiceType, stream, user, game);
            else
                return null;
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotStream?> GetStream_Force(ILiveBotUser user)
        {
            TwitchLibStreams.Stream? stream;
            if (Monitor.LiveStreams.ContainsKey(user.Id))
                stream = Monitor.LiveStreams[user.Id];
            else
                stream = await API_GetStream(user);
            if (stream == null) return null;
            ILiveBotGame game = await GetGame(stream.GameId);
            return new TwitchStream(BaseURL, ServiceType, stream, user, game);
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotUser?> GetUserById(string userId)
        {
            if (_userCache.ContainsKey(userId))
                return _userCache[userId];
            User? apiUser = await API_GetUserById(userId);
            if (apiUser == null) return null;
            var twitchUser = new TwitchUser(BaseURL, ServiceType, apiUser);
            _userCache.TryAdd(userId, twitchUser);
            return twitchUser;
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotUser?> GetUser(string? username = null, string? userId = null, string? profileURL = null)
        {
            User? apiUser;

            if (username == null && userId == null && profileURL == null)
                return null;

            var cachedUser = _userCache.Values.Where(i => i.Id == userId || i.Username?.ToLower() == username?.ToLower() || i.ProfileURL?.ToLower() == profileURL?.ToLower()).FirstOrDefault();
            if (cachedUser != null)
                return cachedUser;

            if (!string.IsNullOrEmpty(username))
            {
                apiUser = await API_GetUserByLogin(username: username);
            }
            else if (!string.IsNullOrEmpty(userId))
            {
                apiUser = await API_GetUserById(userId: userId);
            }
            else if (!string.IsNullOrEmpty(profileURL))
            {
                apiUser = await API_GetUserByURL(url: profileURL);
            }
            else
            {
                return null;
            }
            if (apiUser == null) return null;
            TwitchUser twitchUser = new TwitchUser(BaseURL, ServiceType, apiUser);
            _userCache.TryAdd(twitchUser.Id, twitchUser);
            return twitchUser;
        }

        /// <inheritdoc/>
        public override bool AddChannel(ILiveBotUser user)
        {
            var channels = Monitor.ChannelsToMonitor;
            if (!channels.Contains(user.Id))
            {
                channels.Add(user.Id);
                Monitor.SetChannelsById(channels);
            }

            if (Monitor.ChannelsToMonitor.Contains(user.Id))
                return true;
            return false;
        }

        /// <inheritdoc/>
        public override bool RemoveChannel(ILiveBotUser user)
        {
            var channels = Monitor.ChannelsToMonitor;
            if (channels.Contains(user.Id))
            {
                channels.Remove(user.Id);
                Monitor.SetChannelsById(channels);
            }

            if (!Monitor.ChannelsToMonitor.Contains(user.Id))
                return true;
            return false;
        }

        #endregion Interface Requirements
    }
}