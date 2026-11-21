using System.Collections.Concurrent;
using System.Timers;
using LiveBot.Core.Cache;
using LiveBot.Core.Repository.Base.Monitor;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;
using LiveBot.Watcher.Twitch.Contracts;
using LiveBot.Watcher.Twitch.Models;
using MassTransit;
using StackExchange.Redis;
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
        public IBus _bus;
        public bool IsWatcher = false;
        private readonly IConfiguration _configuration;
        private readonly ConnectionMultiplexer _cache;
        private readonly int RetryDelay = 1000 * 5; // 5 seconds
        private readonly int ApiRetryCount = 5; // How many times to retry API requests
        private readonly string _authCacheName = "twitch:auth";
        private readonly string _gameCacheName = "twitch:games";
        private readonly string _userCacheName = "twitch:users";

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
        public System.Timers.Timer RefreshMonitoredUsersTimer;
        private System.Timers.Timer? _userRetryTimer;
        private int _userRetryAttempts;
        private readonly SemaphoreSlim _userUpdateLock = new SemaphoreSlim(1, 1);

        // My caches
        private ConcurrentDictionary<string, ILiveBotGame> _gameCache = new ConcurrentDictionary<string, ILiveBotGame>();

        private ConcurrentDictionary<string, ILiveBotUser> _userCache = new ConcurrentDictionary<string, ILiveBotUser>();

        /// <summary>
        /// Represents the whole Service for Twitch Monitoring
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public TwitchMonitor(ILogger<TwitchMonitor> logger, IUnitOfWorkFactory factory, IBus bus, IConfiguration configuration, ConnectionMultiplexer cache)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            _logger = logger;
            _factory = factory;
            _bus = bus;
            _configuration = configuration;
            _cache = cache;

            IsEnabled = true;
            StartTime = DateTime.UtcNow;
            BaseURL = "https://twitch.tv";
            ServiceType = ServiceEnum.Twitch;
            URLPattern = ServiceUtils.TwitchUrlPattern;

            var rateLimiter = TimeLimiter.GetFromMaxCountByInterval(800, TimeSpan.FromMinutes(1));
            API = new TwitchAPI(rateLimiter: rateLimiter);
            Monitor = new LiveStreamMonitorService(api: API, checkIntervalInSeconds: 60, maxStreamRequestCountPerRequest: 100);

            ClientId = _configuration.GetValue<string>("TwitchClientId") ?? "";
            ClientSecret = _configuration.GetValue<string>("TwitchClientSecret") ?? "";

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
            _logger.LogDebug("Monitor service successfully connected to Twitch! {ServiceType}", ServiceType);
        }

        // Handler signatures are fixed as void by TwitchLib and System.Timers, and an exception escaping an async void
        // method is rethrown on the thread pool where it terminates the process.
        private async void FireAndLog(Func<Task> work, string operation)
        {
            try
            {
                await work();
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Error running {Operation} for {ServiceType}", operation, ServiceType);
            }
        }

        public async void Monitor_OnStreamOnline(object? sender, OnStreamOnlineArgs e)
        {
            try
            {
                if (!IsWatcher) return;

                ILiveBotUser? user = await GetUserById(e.Stream.UserId);
                if (user == null)
                {
                    _logger.LogWarning("Could not find user {UserId} for online event", e.Stream.UserId);
                    return;
                }

                ILiveBotGame game = await GetGame(e.Stream.GameId);
                ILiveBotStream stream = new TwitchStream(BaseURL, ServiceType, e.Stream, user, game);

                await PublishStreamOnline(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Error handling {ServiceType} stream online for {UserId}", ServiceType, e.Stream?.UserId);
            }
        }

        public async void Monitor_OnStreamUpdate(object? sender, OnStreamUpdateArgs e)
        {
            try
            {
                if (!IsWatcher) return;

                ILiveBotUser? user = await GetUserById(e.Stream.UserId);
                if (user == null)
                {
                    _logger.LogWarning("Could not find user {UserId} for update event", e.Stream.UserId);
                    return;
                }

                ILiveBotGame game = await GetGame(e.Stream.GameId);
                ILiveBotStream stream = new TwitchStream(BaseURL, ServiceType, e.Stream, user, game);

                await PublishStreamUpdate(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Error handling {ServiceType} stream update for {UserId}", ServiceType, e.Stream?.UserId);
            }
        }

        public async void Monitor_OnStreamOffline(object? sender, OnStreamOfflineArgs e)
        {
            try
            {
                if (!IsWatcher) return;

                ILiveBotUser? user = await GetUserById(e.Stream.UserId);
                if (user == null)
                {
                    _logger.LogWarning("Could not find user {UserId} for offline event", e.Stream.UserId);
                    return;
                }

                ILiveBotGame game = await GetGame(e.Stream.GameId);
                ILiveBotStream stream = new TwitchStream(BaseURL, ServiceType, e.Stream, user, game);

                await PublishStreamOffline(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Error handling {ServiceType} stream offline for {UserId}", ServiceType, e.Stream?.UserId);
            }
        }

        #endregion Events

        #region API Calls

        /// <summary>
        /// Runs a Twitch API call, retrying transient (5xx) and auth failures within one shared attempt budget.
        /// Returns null once <see cref="ApiRetryCount"/> attempts have failed.
        /// </summary>
        private async Task<T?> CallTwitchAsync<T>(Func<Task<T?>> call, string operation) where T : class
        {
            for (int attempt = 1; attempt <= ApiRetryCount; attempt++)
            {
                bool refreshAuth = false;
                try
                {
                    await WaitForAuthUnlockAsync();
                    return await call();
                }
                catch (Exception e) when (e is BadGatewayException || e is InternalServerErrorException)
                {
                    _logger.LogError(exception: e, message: "Error getting {ServiceType} {Operation}", ServiceType, operation);
                }
                catch (Exception e) when (e is InvalidCredentialException || e is BadScopeException)
                {
                    _logger.LogError(exception: e, message: "Error getting {ServiceType} {Operation}", ServiceType, operation);
                    refreshAuth = true;
                }

                // No point refreshing credentials or sleeping when there is no attempt left to use them.
                if (attempt == ApiRetryCount) break;

                if (refreshAuth)
                {
                    await UpdateAuth(force: true);
                }
                await Task.Delay(RetryDelay);
            }
            return null;
        }

        private Task<Game?> API_GetGame(string gameId)
        {
            return CallTwitchAsync<Game>(async () =>
            {
                var gameIDs = new List<string> { gameId };
                GetGamesResponse games = await API.Helix.Games.GetGamesAsync(gameIds: gameIDs).ConfigureAwait(false);
                return games.Games.FirstOrDefault(i => i.Id == gameId);
            }, "Game");
        }

        private Task<User?> API_GetUserByLogin(string username)
        {
            return CallTwitchAsync<User>(async () =>
            {
                var usernameList = new List<string> { username.ToLower() };
                GetUsersResponse apiUser = await API.Helix.Users.GetUsersAsync(logins: usernameList).ConfigureAwait(false);
                return apiUser.Users.FirstOrDefault(i => string.Equals(i.Login, username, StringComparison.OrdinalIgnoreCase));
            }, "User by Login");
        }

        private Task<User?> API_GetUserById(string userId)
        {
            return CallTwitchAsync<User>(async () =>
            {
                var userIdList = new List<string> { userId };
                GetUsersResponse apiUser = await API.Helix.Users.GetUsersAsync(ids: userIdList).ConfigureAwait(false);
                return apiUser.Users.FirstOrDefault(i => i.Id == userId);
            }, "User by Id");
        }

        private Task<GetUsersResponse?> API_GetUsersById(List<string> userIdList)
        {
            return CallTwitchAsync<GetUsersResponse>(async () =>
            {
                return await API.Helix.Users.GetUsersAsync(ids: userIdList).ConfigureAwait(false);
            }, "Users by Id");
        }

        private Task<User?> API_GetUserByURL(string url)
        {
            string username = GetURLRegex(URLPattern).Match(url).Groups["username"].ToString().ToLower();
            return API_GetUserByLogin(username: username);
        }

        private Task<TwitchLibStreams.Stream?> API_GetStream(ILiveBotUser user)
        {
            return CallTwitchAsync<TwitchLibStreams.Stream>(async () =>
            {
                var userIds = new List<string>
                {
                    user.Id
                };
                var streams = await API.Helix.Streams.GetStreamsAsync(type: "live", userIds: userIds);
                return streams.Streams.Where(i => i.UserId == user.Id).FirstOrDefault();
            }, "Stream");
        }

        #endregion API Calls

        #region Misc Functions

        /// <summary>
        /// Wait for an Auth lock to not be in place
        /// </summary>
        /// <returns></returns>
        private async Task WaitForAuthUnlockAsync()
        {
            bool authLocked;
            do
            {
                authLocked = await _cache.CheckForLockAsync(recordId: _authCacheName);
            }
            while (authLocked);
        }

        private static IEnumerable<List<T>> SplitList<T>(List<T> locations, int nSize = 30)
        {
            for (int i = 0; i < locations.Count; i += nSize)
            {
                yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
            }
        }

        private async Task<TwitchAuth> GetAuthAsync()
        {
            var auth = await _cache.GetRecordAsync<TwitchAuth>(recordId: _authCacheName);
            if (auth is null || auth.Expired || auth.ExpiresAt <= DateTime.UtcNow)
            {
                var tempAuth = await _work.AuthRepository.SingleOrDefaultAsync(i => i.ServiceType == ServiceType && i.ClientId == ClientId && i.Expired == false);

                auth = new TwitchAuth
                {
                    Id = tempAuth.Id,
                    Deleted = tempAuth.Deleted,
                    TimeStamp = tempAuth.TimeStamp,
                    ServiceType = tempAuth.ServiceType,
                    Expired = tempAuth.Expired,
                    ClientId = tempAuth.ClientId,
                    AccessToken = tempAuth.AccessToken,
                    RefreshToken = tempAuth.RefreshToken,
                    ExpiresAt = tempAuth.ExpiresAt,
                };

                if (auth.ExpiresAt == DateTime.MinValue)
                {
                    // if it's already expired, or is not set add 1 minutes (probably refreshing anyways)
                    auth.ExpiresAt = DateTime.UtcNow.AddMinutes(1);
                    await _work.AuthRepository.UpdateAsync(auth);
                }

                if (auth.ExpiresAt > DateTime.UtcNow)
                {
                    // Only set cache if the ExpiresAt is in the future
                    // Otherwise it throws an error about being negative
                    var timeToExpire = auth.ExpiresAt - DateTime.UtcNow;
                    await _cache.SetRecordAsync<TwitchAuth>(recordId: _authCacheName, data: auth, expiryTime: timeToExpire.Duration());
                }
            }
            return auth;
        }

        public async Task<TwitchAuth> GetAndSetActiveAuth()
        {
            //var activeAuth = await _work.AuthRepository.SingleOrDefaultAsync(i => i.ServiceType == ServiceType && i.ClientId == ClientId && i.Expired == false);
            var activeAuth = await GetAuthAsync();
            if (AccessToken != activeAuth.AccessToken)
                AccessToken = activeAuth.AccessToken;
            return activeAuth;
        }

        public async Task UpdateAuth(bool force = false)
        {
            _logger.LogInformation(message: "Updating Auth for {ServiceType} with Client Id {ClientId}", ServiceType, ClientId);
            if (!IsWatcher)
            {
                try
                {
                    var activeAuth = await GetAndSetActiveAuth();
                    _logger.LogInformation(message: "Successfully set AccessToken for {ServiceType} with Client Id {ClientId} to active auth", ServiceType, ClientId);

                    // Trigger it 5 minutes before expiration time to be safe
                    var timeToExpire = activeAuth.ExpiresAt - DateTime.UtcNow;
                    SetupAuthTimer(timeToExpire);
                }
                catch (Exception ex)
                {
                    _logger.LogError(exception: ex, message: "Unable to update AccessToken for {ServiceType} with Client Id {ClientId}", ServiceType, ClientId);
                }
            }
            else
            {
                _logger.LogDebug(message: "Refreshing Auth for {ServiceType}", ServiceType);

                var oldAuth = await GetAndSetActiveAuth();

                var obtainedLock = false;
                Guid lockGuid = Guid.NewGuid();

                if (oldAuth.Expired || oldAuth.ExpiresAt <= DateTime.UtcNow || force)
                {
                    do
                    {
                        // Obtain a lock for a maximum of 30 seconds
                        obtainedLock = await _cache.ObtainLockAsync(recordId: _authCacheName, identifier: lockGuid, expiryTime: TimeSpan.FromSeconds(30));
                    }
                    while (!obtainedLock);

                    try
                    {
                        oldAuth = await GetAuthAsync();
                        if (oldAuth.Expired || oldAuth.ExpiresAt <= DateTime.UtcNow)
                        {
                            RefreshResponse refreshResponse = await API.Auth.RefreshAuthTokenAsync(refreshToken: oldAuth.RefreshToken, clientSecret: ClientSecret, clientId: ClientId);

                            var newAuth = new TwitchAuth(ServiceType, ClientId, refreshResponse);
                            if (refreshResponse.ExpiresIn < 1800)
                            {
                                newAuth.ExpiresAt = DateTime.UtcNow.AddSeconds(1800);
                            }

                            await _work.AuthRepository.AddOrUpdateAsync(newAuth, i => i.ServiceType == ServiceType && i.ClientId == ClientId && i.AccessToken == newAuth.AccessToken);

                            var timeToExpire = newAuth.ExpiresAt - DateTime.UtcNow;
                            await _cache.SetRecordAsync<TwitchAuth>(recordId: _authCacheName, data: newAuth, expiryTime: timeToExpire.Duration());

                            oldAuth.Expired = true;
                            await _work.AuthRepository.AddOrUpdateAsync(oldAuth, i => i.ServiceType == ServiceType && i.ClientId == ClientId && i.AccessToken == oldAuth.AccessToken);
                            AccessToken = newAuth.AccessToken;

                            _logger.LogDebug("{ServiceType} Expiration time: {ExpirationSeconds}", ServiceType, refreshResponse.ExpiresIn < 1800 ? 1800 : refreshResponse.ExpiresIn);

                            // Force all other tokens to be expired
                            var oldAuths = await _work.AuthRepository.FindAsync(i => i.ServiceType == ServiceType && i.ClientId == ClientId && i.Expired == false && i.AccessToken != newAuth.AccessToken);
                            foreach (var auth in oldAuths)
                            {
                                auth.Expired = true;
                                await _work.AuthRepository.UpdateAsync(auth);
                            }
                        }
                    }
                    finally
                    {
                        await _cache.ReleaseLockAsync(recordId: _authCacheName, identifier: lockGuid);
                    }
                }
                var tempAuth = await GetAndSetActiveAuth();
                // Trigger it at expiration time to be safe
                SetupAuthTimer(tempAuth.ExpiresAt - DateTime.UtcNow);
            }
        }

        private void SetupAuthTimer(TimeSpan timeSpan)
        {
            RefreshAuthTimer?.Dispose();

            if (timeSpan.TotalSeconds < 1800)
                timeSpan = TimeSpan.FromSeconds(1800);

            RefreshAuthTimer = new System.Timers.Timer(timeSpan.Duration().TotalMilliseconds)
            {
                AutoReset = false
            };
            RefreshAuthTimer.Elapsed += (_, _) => FireAndLog(() => UpdateAuth(), nameof(UpdateAuth));
            RefreshAuthTimer.Start();
        }

        public async Task UpdateUsers()
        {
            // Skip rather than queue: the hourly tick and a retry must not run side by side.
            if (!await _userUpdateLock.WaitAsync(0)) return;

            bool failed = false;
            try
            {
                foreach (List<string> userIds in SplitList(Monitor.ChannelsToMonitor, nSize: 100))
                {
                    GetUsersResponse? users = await API_GetUsersById(userIds);
                    if (users == null)
                    {
                        failed = true;
                        _logger.LogWarning(message: "No response updating a batch of {ServiceType} users", ServiceType);
                        continue;
                    }
                    foreach (User user in users.Users)
                    {
                        var twitchUser = new TwitchUser(BaseURL, ServiceType, user);
                        if (_userCache.ContainsKey(user.Id))
                        {
                            _userCache[user.Id] = twitchUser;
                        }
                        else
                        {
                            _userCache.TryAdd(user.Id, twitchUser);
                        }
                        await _cache.SetListItemAsync<TwitchUser>(recordId: _userCacheName, fieldName: twitchUser.Id, data: twitchUser);
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
                            _logger.LogError(exception: e, message: "Error updating user {ServiceType} {Username}", ServiceType, twitchUser.Username);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                failed = true;
                _logger.LogError(exception: e, message: "Error updating {ServiceType} users", ServiceType);
            }
            finally
            {
                // Retry state changes while the lock is held so overlapping runs cannot dispose or reschedule each other's timer.
                try
                {
                    if (failed)
                    {
                        ScheduleUserRetry();
                    }
                    else
                    {
                        // A successful run makes any pending retry redundant.
                        Interlocked.Exchange(ref _userRetryAttempts, 0);
                        Interlocked.Exchange(ref _userRetryTimer, null)?.Dispose();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(exception: e, message: "Error updating retry state for {ServiceType} users", ServiceType);
                }
                finally
                {
                    _userUpdateLock.Release();
                }
            }
        }

        private void SetupUserTimer(double minutes = 60)
        {
            RefreshUsersTimer?.Dispose();

            RefreshUsersTimer = new System.Timers.Timer()
            {
                AutoReset = true,
                Enabled = true,
                Interval = TimeSpan.FromMinutes(minutes).TotalMilliseconds
            };
            RefreshUsersTimer.Elapsed += (_, _) => FireAndLog(UpdateUsers, nameof(UpdateUsers));
        }

        // One-shot so the hourly RefreshUsersTimer keeps its interval; a failed retry schedules the next one itself.
        private void ScheduleUserRetry()
        {
            // No attempt limit so recovery is picked up during long outages; the 30 minute cap bounds the wait.
            int attempt = Interlocked.Increment(ref _userRetryAttempts);
            int backoffMinutes = Math.Min(1 << Math.Min(attempt - 1, 5), 30);
            TimeSpan delay = TimeSpan.FromMinutes(backoffMinutes) + TimeSpan.FromSeconds(Random.Shared.Next(0, 11));

            var retryTimer = new System.Timers.Timer(delay.TotalMilliseconds)
            {
                AutoReset = false
            };
            retryTimer.Elapsed += (_, _) => FireAndLog(UpdateUsers, nameof(UpdateUsers));
            Interlocked.Exchange(ref _userRetryTimer, retryTimer)?.Dispose();
            retryTimer.Start();
        }

        public async void ClearCache(object? sender, ElapsedEventArgs? e)
        {
            try
            {
                _userCache.Clear();
                await _cache.DeleteListAsync(recordId: _userCacheName);

                _gameCache.Clear();
                await _cache.DeleteListAsync(recordId: _gameCacheName);
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Error clearing {ServiceType} caches", ServiceType);
            }
        }

        private void SetupCacheTimer()
        {
            ClearCacheTimer?.Dispose();
            ClearCacheTimer = new System.Timers.Timer()
            {
                AutoReset = true,
                Enabled = true,
                Interval = TimeSpan.FromMinutes(60).TotalMilliseconds
            };
            ClearCacheTimer.Elapsed += ClearCache;
        }

        private async Task RefreshMonitoredUsers()
        {
            var streamsubscriptions = await _work.SubscriptionRepository.FindAsync(i => i.User.ServiceType == ServiceType);
            var channelList = streamsubscriptions.Select(i => i.User.SourceID).Distinct().ToList();

            if (channelList.Count == 0)
                // Add myself so startup doesn't fail if there's no users in the database
                channelList.Add("22812120");
            Monitor.SetChannelsById(channelList);
        }

        private void SetupRefreshMonitoredUsers(double minutes = 1) => SetupRefreshMonitoredUsers(timeSpan: TimeSpan.FromMinutes(minutes));

        private void SetupRefreshMonitoredUsers(TimeSpan timeSpan)
        {
            RefreshMonitoredUsersTimer?.Dispose();
            RefreshMonitoredUsersTimer = new System.Timers.Timer()
            {
                AutoReset = true,
                Enabled = true,
                Interval = timeSpan.TotalMilliseconds
            };
            RefreshMonitoredUsersTimer.Elapsed += (_, _) => FireAndLog(RefreshMonitoredUsers, nameof(RefreshMonitoredUsers));
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
                _logger.LogError(exception: e, message: "Error trying to publish {ServiceType} StreamOnline", ServiceType);
            }
        }

        public async Task PublishStreamUpdate(ILiveBotStream stream)
        {
            try
            {
                await _bus.Publish(new TwitchStreamUpdate { Stream = stream });
            }
            catch (Exception e)
            {
                _logger.LogError(exception: e, message: "Error trying to publish {ServiceType} StreamUpdate", ServiceType);
            }
        }

        public async Task PublishStreamOffline(ILiveBotStream stream)
        {
            try
            {
                await _bus.Publish(new TwitchStreamOffline { Stream = stream });
            }
            catch (Exception e)
            {
                _logger.LogError(exception: e, message: "Error trying to publish {ServiceType} StreamOffline", ServiceType);
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
                await RefreshMonitoredUsers();
                SetupUserTimer();
                SetupCacheTimer();
                SetupRefreshMonitoredUsers();

                // Perform startup catch-up before starting the monitor
                await PerformStartupCatchup();

                await Task.Run(Monitor.Start);
            }
        }

        /// <summary>
        /// Performs startup catch-up to check for streams that changed state while the watcher was offline
        /// </summary>
        private async Task PerformStartupCatchup()
        {
            try
            {
                _logger.LogInformation("Starting startup catch-up for {ServiceType}", ServiceType);

                // Get all users that have subscriptions
                var streamSubscriptions = await _work.SubscriptionRepository.FindAsync(i => i.User.ServiceType == ServiceType);
                var distinctUsers = streamSubscriptions
                    .Select(s => s.User)
                    .DistinctBy(u => u.SourceID)
                    .ToList();

                if (!distinctUsers.Any())
                {
                    _logger.LogInformation("No users to catch up for {ServiceType}", ServiceType);
                    return;
                }

                // Convert to ILiveBotUser objects
                var liveBotUsers = new List<ILiveBotUser>();
                foreach (var dbUser in distinctUsers)
                {
                    try
                    {
                        var liveBotUser = await GetUserById(dbUser.SourceID);
                        if (liveBotUser != null)
                        {
                            liveBotUsers.Add(liveBotUser);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get user data for {UserId} during startup catch-up", dbUser.SourceID);
                    }
                }

                if (liveBotUsers.Any())
                {
                    // Publish startup catch-up request
                    var catchupRequest = new TwitchStartupCatchup
                    {
                        ServiceType = ServiceType,
                        StreamUsers = liveBotUsers
                    };

                    await _bus.Publish(catchupRequest);
                    _logger.LogInformation("Published startup catch-up request for {UserCount} users on {ServiceType}",
                        liveBotUsers.Count, ServiceType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during startup catch-up for {ServiceType}", ServiceType);
                // Don't throw - we don't want to prevent the watcher from starting
            }
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotGame> GetGame(string gameId)
        {
            var cachedGame = await _cache.GetListItemAsync<TwitchGame>(recordId: _gameCacheName, fieldName: gameId);
            if (cachedGame is not null)
                return cachedGame;
            Game? game = null;
            if (!string.IsNullOrWhiteSpace(gameId))
            {
                game = await API_GetGame(gameId);
            }
            var twitchGame = new TwitchGame(BaseURL, ServiceType, game);
            // API_GetGame returns null after exhausting retries, so caching a null result would pin a placeholder until ClearCache.
            if (game is not null || string.IsNullOrWhiteSpace(gameId))
            {
                await _cache.SetListItemAsync<TwitchGame>(recordId: _gameCacheName, fieldName: gameId, data: twitchGame);
            }
            return twitchGame;
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotStream?> GetStream(ILiveBotUser user)
        {
            if (!Monitor.LiveStreams.TryGetValue(user.Id, out TwitchLibStreams.Stream? stream))
                return null;
            ILiveBotGame game = await GetGame(stream.GameId);
            return new TwitchStream(BaseURL, ServiceType, stream, user, game);
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotStream?> GetStream(string userId)
        {
            if (!Monitor.LiveStreams.TryGetValue(userId, out TwitchLibStreams.Stream? stream))
                return null;
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
            if (!Monitor.LiveStreams.TryGetValue(user.Id, out stream))
                stream = await API_GetStream(user);
            if (stream == null) return null;
            ILiveBotGame game = await GetGame(stream.GameId);
            return new TwitchStream(BaseURL, ServiceType, stream, user, game);
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotUser?> GetUserById(string userId)
        {
            var cachedUser = await _cache.GetListItemAsync<TwitchUser>(recordId: _userCacheName, fieldName: userId);
            if (cachedUser is not null)
                return cachedUser;
            User? apiUser = await API_GetUserById(userId);
            if (apiUser == null) return null;
            var twitchUser = new TwitchUser(BaseURL, ServiceType, apiUser);
            await _cache.SetListItemAsync<TwitchUser>(recordId: _userCacheName, fieldName: userId, data: twitchUser);
            return twitchUser;
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotUser?> GetUser(string? username = null, string? userId = null, string? profileURL = null)
        {
            User? apiUser;

            if (username == null && userId == null && profileURL == null)
                return null;

            if (!string.IsNullOrWhiteSpace(userId) && _userCache.TryGetValue(userId, out var cachedById))
                return cachedById;

            // Only compare fields the caller supplied; otherwise a null argument matches a cached user whose field is also null.
            var cachedUser = _userCache.Values.FirstOrDefault(i =>
                (!string.IsNullOrWhiteSpace(userId) && i.Id == userId) ||
                (!string.IsNullOrWhiteSpace(username) && string.Equals(i.Username, username, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(profileURL) && string.Equals(i.ProfileURL, profileURL, StringComparison.OrdinalIgnoreCase)));
            if (cachedUser != null)
                return cachedUser;

            if (!string.IsNullOrEmpty(username))
            {
                apiUser = await API_GetUserByLogin(username: username);
            }
            else if (!string.IsNullOrEmpty(userId))
            {
                return await GetUserById(userId: userId);
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
            var twitchUser = new TwitchUser(BaseURL, ServiceType, apiUser);
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
