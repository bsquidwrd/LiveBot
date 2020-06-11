using LiveBot.Core.Repository.Base.Monitor;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;
using LiveBot.Watcher.Twitch.Contracts;
using LiveBot.Watcher.Twitch.Models;
using MassTransit;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Core.RateLimiter;
using TwitchLib.Api.Helix.Models.Games;
using TwitchLib.Api.Helix.Models.Streams;
using TwitchLib.Api.Helix.Models.Users;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;
using TwitchLib.Api.V5.Models.Auth;

namespace LiveBot.Watcher.Twitch
{
    public class TwitchMonitor : BaseLiveBotMonitor
    {
        public LiveStreamMonitorService Monitor;
        public TwitchAPI API;
        public IServiceProvider services;
        public IBusControl _bus;
        private readonly int RetryDelay = 1000 * 5;

        private IUnitOfWork _workGame;
        private IUnitOfWork _workUser;

        private IUnitOfWork GameWork
        {
            set => _workGame = value;
            get
            {
                if (_workGame == null)
                {
                    _workGame = _factory.Create();
                }
                return _workGame;
            }
        }

        private IUnitOfWork UserWork
        {
            set => _workUser = value;
            get
            {
                if (_workUser == null)
                {
                    _workUser = _factory.Create();
                }
                return _workUser;
            }
        }

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

        /// <summary>
        /// Represents the whole Service for Twitch Monitoring
        /// </summary>
        public TwitchMonitor()
        {
            BaseURL = "https://twitch.tv";
            ServiceType = ServiceEnum.TWITCH;
            URLPattern = "^((http|https):\\/\\/|)([\\w\\d]+\\.)?twitch\\.tv/(?<username>[a-zA-Z0-9_]{1,})";

            var rateLimiter = TimeLimiter.GetFromMaxCountByInterval(800, TimeSpan.FromMinutes(1));
            API = new TwitchAPI(rateLimiter: rateLimiter);
            Monitor = new LiveStreamMonitorService(api: API, checkIntervalInSeconds: 60, maxStreamRequestCountPerRequest: 100);

            Monitor.OnServiceStarted += Monitor_OnServiceStarted;
            Monitor.OnStreamOnline += Monitor_OnStreamOnline;
            Monitor.OnStreamOffline += Monitor_OnStreamOffline;
            Monitor.OnStreamUpdate += Monitor_OnStreamUpdate;
        }

        #region Events

        public void Monitor_OnServiceStarted(object sender, OnServiceStartedArgs e)
        {
            Log.Debug("Monitor service successfully connected to Twitch!");
        }

        public async void Monitor_OnStreamOnline(object sender, OnStreamOnlineArgs e)
        {
            ILiveBotStream stream = await GetStream(e.Stream);
            //await _PublishUpdateUser(stream.User);
            await _PublishStreamOnline(stream);
        }

        public async void Monitor_OnStreamUpdate(object sender, OnStreamUpdateArgs e)
        {
            ILiveBotStream stream = await GetStream(e.Stream);
            await _PublishStreamOffline(stream);
        }

        public async void Monitor_OnStreamOffline(object sender, OnStreamOfflineArgs e)
        {
            ILiveBotStream stream = await GetStream(e.Stream);
            await _PublishStreamOffline(stream);
        }

        #endregion Events

        #region API Calls

        private async Task<Game> API_GetGame(string gameId)
        {
            try
            {
                List<string> gameIDs = new List<string> { gameId };
                GetGamesResponse games = await API.Helix.Games.GetGamesAsync(gameIds: gameIDs).ConfigureAwait(false);
                return games.Games.FirstOrDefault(i => i.Id == gameId);
            }
            catch (BadGatewayException e)
            {
                Log.Error($"{e}");
                await Task.Delay(RetryDelay);
                return await API_GetGame(gameId);
            }
            catch (InvalidCredentialException e)
            {
                Log.Error($"{e}");
                await _PublishMonitorRefreshAuth();
                await Task.Delay(RetryDelay);
                return await API_GetGame(gameId);
            }
        }

        private async Task<User> API_GetUserByLogin(string username)
        {
            try
            {
                List<string> usernameList = new List<string> { username };
                GetUsersResponse apiUser = await API.Helix.Users.GetUsersAsync(logins: usernameList).ConfigureAwait(false);
                return apiUser.Users.FirstOrDefault(i => i.Login == username);
            }
            catch (BadGatewayException e)
            {
                Log.Error($"{e}");
                await Task.Delay(RetryDelay);
                return await API_GetUserByLogin(username);
            }
            catch (InvalidCredentialException e)
            {
                Log.Error($"{e}");
                await _PublishMonitorRefreshAuth();
                await Task.Delay(RetryDelay);
                return await API_GetUserByLogin(username);
            }
        }

        private async Task<User> API_GetUserById(string userId)
        {
            try
            {
                List<string> userIdList = new List<string> { userId };
                GetUsersResponse apiUser = await API.Helix.Users.GetUsersAsync(ids: userIdList).ConfigureAwait(false);
                return apiUser.Users.FirstOrDefault(i => i.Id == userId);
            }
            catch (BadGatewayException e)
            {
                Log.Error($"{e}");
                await Task.Delay(RetryDelay);
                return await API_GetUserById(userId);
            }
            catch (InvalidCredentialException e)
            {
                Log.Error($"{e}");
                await _PublishMonitorRefreshAuth();
                await Task.Delay(RetryDelay);
                return await API_GetUserById(userId);
            }
        }

        private async Task<GetUsersResponse> API_GetUsersById(List<string> userIdList)
        {
            try
            {
                GetUsersResponse apiUsers = await API.Helix.Users.GetUsersAsync(ids: userIdList).ConfigureAwait(false);
                return apiUsers;
            }
            catch (BadGatewayException e)
            {
                Log.Error($"{e}");
                await Task.Delay(RetryDelay);
                return await API_GetUsersById(userIdList);
            }
            catch (InvalidCredentialException e)
            {
                Log.Error($"{e}");
                await _PublishMonitorRefreshAuth();
                await Task.Delay(RetryDelay);
                return await API_GetUsersById(userIdList);
            }
        }

        private async Task<User> API_GetUserByURL(string url)
        {
            try
            {
                string username = GetURLRegex(URLPattern).Match(url).Groups["username"].ToString();
                return await API_GetUserByLogin(username: username);
            }
            catch (BadGatewayException e)
            {
                Log.Error($"{e}");
                await Task.Delay(RetryDelay);
                return await API_GetUserByURL(url);
            }
            catch (InvalidCredentialException e)
            {
                Log.Error($"{e}");
                await _PublishMonitorRefreshAuth();
                await Task.Delay(RetryDelay);
                return await API_GetUserByURL(url);
            }
        }

        private async Task<Stream> API_GetStream(string userId)
        {
            try
            {
                List<string> userIdList = new List<string> { userId };
                GetStreamsResponse streams = await API.Helix.Streams.GetStreamsAsync(userIds: userIdList);
                return streams.Streams.FirstOrDefault(i => i.UserId == userId);
            }
            catch (BadGatewayException e)
            {
                Log.Error($"{e}");
                await Task.Delay(RetryDelay);
                return await API_GetStream(userId);
            }
            catch (InvalidCredentialException e)
            {
                Log.Error($"{e}");
                await _PublishMonitorRefreshAuth();
                await Task.Delay(RetryDelay);
                return await API_GetStream(userId);
            }
        }

        #endregion API Calls

        #region Misc Functions

        public async Task<ILiveBotStream> GetStream(Stream stream)
        {
            ILiveBotUser liveBotUser = await GetUser(userId: stream.UserId);
            ILiveBotGame liveBotGame = await GetGame(gameId: stream.GameId);
            return new TwitchStream(BaseURL, ServiceType, stream, liveBotUser, liveBotGame);
        }

        #endregion Misc Functions

        #region Messaging Implementation

        public async Task _PublishStreamOnline(ILiveBotStream stream)
        {
            try
            {
                await _bus.Publish(new TwitchStreamOnline { Stream = stream });
            }
            catch (Exception e)
            {
                Log.Error($"Error trying to publish StreamOnline:\n{e}");
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
                Log.Error($"Error trying to publish StreamUpdate:\n{e}");
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
                Log.Error($"Error trying to publish StreamOffline:\n{e}");
            }
        }

        public async Task _PublishMonitorRefreshAuth()
        {
            try
            {
                await _bus.Publish(new TwitchRefreshAuth { ServiceType = ServiceType, ClientId = ClientId });
            }
            catch (Exception e)
            {
                Log.Error($"Error trying to publish TwitchRefreshAuth:\n{e}");
            }
        }

        public async Task _PublishUpdateUser(ILiveBotUser user)
        {
            try
            {
                await _bus.Publish(new TwitchUpdateUser(ServiceType, user));
            }
            catch (Exception e)
            {
                Log.Error($"Error trying to publish TwitchUpdateUser:\n{e}");
            }
        }

        #endregion Messaging Implementation

        #region Interface Requirements

        /// <inheritdoc/>
        public override ILiveBotMonitorStart GetStartClass()
        {
            return new TwitchStart();
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotGame> GetGame(string gameId)
        {
            Game game;

            //StreamGame existingGame = await GameWork.GameRepository.SingleOrDefaultAsync(i => i.ServiceType == ServiceType && i.SourceId == gameId);
            StreamGame existingGame = null;
            if (existingGame != null)
            {
                if (DateTime.UtcNow.Subtract(existingGame.TimeStamp).TotalHours > 1)
                {
                    game = await API_GetGame(gameId);
                    TwitchGame twitchGame = new TwitchGame(BaseURL, ServiceType, game);
                    StreamGame streamGame = new StreamGame
                    {
                        ServiceType = ServiceType,
                        SourceId = twitchGame.Id,
                        Name = twitchGame.Name,
                        ThumbnailURL = twitchGame.ThumbnailURL
                    };
                    await _workGame.GameRepository.AddOrUpdateAsync(streamGame, i => (i.ServiceType == ServiceType && i.SourceId == gameId));
                    return twitchGame;
                }
                else
                {
                    return new TwitchGame(BaseURL, ServiceType, existingGame);
                }
            }
            game = await API_GetGame(gameId);
            return new TwitchGame(BaseURL, ServiceType, game);
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotStream> GetStream(ILiveBotUser user)
        {
            Stream stream = await API_GetStream(user.Id);
            if (stream == null)
                return null;
            ILiveBotGame game = await GetGame(stream.GameId);
            return new TwitchStream(BaseURL, ServiceType, stream, user, game);
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotUser> GetUserById(string userId)
        {
            User apiUser = await API_GetUserById(userId);
            return new TwitchUser(BaseURL, ServiceType, apiUser);
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotUser> GetUser(string username = null, string userId = null, string profileURL = null)
        {
            User apiUser;
            //StreamUser streamUser = await UserWork.UserRepository.SingleOrDefaultAsync(i => i.ServiceType == ServiceType && (i.Username == username || i.SourceID == userId || i.ProfileURL == profileURL));
            //if (streamUser != null)
            //{
            //    return new TwitchUser(BaseURL, ServiceType, streamUser);
            //}

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

            TwitchUser twitchUser = new TwitchUser(BaseURL, ServiceType, apiUser);

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

        /// <inheritdoc/>
        public override async Task<MonitorAuth> UpdateAuth(MonitorAuth oldMonitorAuth)
        {
            RefreshResponse refreshResponse = await API.V5.Auth.RefreshAuthTokenAsync(refreshToken: oldMonitorAuth.RefreshToken, clientSecret: ClientSecret, clientId: ClientId);
            AccessToken = refreshResponse.AccessToken;
            return new TwitchAuth(ServiceType, ClientId, refreshResponse);
        }

        #endregion Interface Requirements
    }
}