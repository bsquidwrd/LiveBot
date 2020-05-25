using LiveBot.Core.Repository.Base.Stream;
using LiveBot.Core.Repository.Enums;
using LiveBot.Core.Repository.Interfaces.Stream;
using LiveBot.Watcher.Twitch.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Games;
using TwitchLib.Api.Helix.Models.Streams;
using TwitchLib.Api.Helix.Models.Users;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;

namespace LiveBot.Watcher.Twitch
{
    public class TwitchMonitor : BaseLiveBotMonitor
    {
        public LiveStreamMonitorService Monitor;
        public TwitchAPI API;
        public IServiceProvider services;

        /// <summary>
        /// Represents the whole Service for Twitch Monitoring
        /// </summary>
        public TwitchMonitor()
        {
            BaseURL = "https://twitch.tv";
            ServiceType = ServiceEnum.TWITCH;
            URLPattern = "^((http|https):\\/\\/|)([\\w\\d]+\\.)?twitch\\.tv/(?<username>[a-zA-Z0-9_]{1,})";

            API = new TwitchAPI();
            Monitor = new LiveStreamMonitorService(api: API, checkIntervalInSeconds: 30, maxStreamRequestCountPerRequest: 100);
        }

        // Start Events
        public void Monitor_OnServiceStarted(object sender, OnServiceStartedArgs e)
        {
            Log.Debug("Monitor service successfully connected to Twitch!");
        }

        public async void Monitor_OnStreamOnline(object sender, OnStreamOnlineArgs e)
        {
            ILiveBotStream stream = await GetStream(e.Stream);
            Log.Debug($"OnStreamOnline: {stream.User} Match: {IsValid(stream.GetStreamURL())}");
        }

        public async void Monitor_OnStreamUpdate(object sender, OnStreamUpdateArgs e)
        {
            ILiveBotStream stream = await GetStream(e.Stream);
            Log.Debug($"OnStreamUpdate: {stream.User}");
            // WHY THE FLYING FUCK IS THIS TRIGGERED EVERYTIME A CHECK IS RUN THROUGH THIS LIB
            // THERE'S LITERALLY NOTHING THAT'S CHANGED, YET SOMETHING IS AND I CAN'T FIGURE IT OUT
            // AHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH
            // Okay I think it's because the stream was previously live on check
            // So I think it sends this incase something has changed to process on my end
        }

        public async void Monitor_OnStreamOffline(object sender, OnStreamOfflineArgs e)
        {
            ILiveBotStream stream = await GetStream(e.Stream);
            Log.Debug($"OnStreamOffline: {stream.User}");
        }

        // End Events

        public async Task<Game> API_GetGame(string gameId)
        {
            List<string> gameIDs = new List<string> { gameId };
            GetGamesResponse games = await API.Helix.Games.GetGamesAsync(gameIds: gameIDs).ConfigureAwait(false);
            return games.Games.FirstOrDefault(i => i.Id == gameId);
        }

        public async Task<User> API_GetUserByLogin(string username)
        {
            List<string> usernameList = new List<string> { username };
            GetUsersResponse apiUser = await API.Helix.Users.GetUsersAsync(logins: usernameList).ConfigureAwait(false);
            return apiUser.Users.FirstOrDefault(i => i.Login == username);
        }

        public async Task<User> API_GetUserById(string userId)
        {
            List<string> userIdList = new List<string> { userId };
            GetUsersResponse apiUser = await API.Helix.Users.GetUsersAsync(ids: userIdList).ConfigureAwait(false);
            return apiUser.Users.FirstOrDefault(i => i.Id == userId);
        }

        public async Task<ILiveBotStream> GetStream(Stream stream)
        {
            Game game = await API_GetGame(stream.GameId);
            ILiveBotUser liveBotUser = await GetUser(userId: stream.UserId);
            ILiveBotGame liveBotGame = new TwitchGame(BaseURL, ServiceType, game);
            return new TwitchStream(BaseURL, ServiceType, stream, liveBotUser, liveBotGame);
        }

        // Implement Interface Requirements
        /// <inheritdoc/>
        public override ILiveBotMonitorStart GetStartClass()
        {
            return new TwitchStart();
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotGame> GetGame(string gameId)
        {
            Game game = await API_GetGame(gameId);
            return new TwitchGame(BaseURL, ServiceType, game);
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotStream> GetStream(ILiveBotUser user)
        {
            List<string> listUserId = new List<string> { user.Id };
            GetStreamsResponse streams = await API.Helix.Streams.GetStreamsAsync(userIds: listUserId);
            Stream stream = streams.Streams.FirstOrDefault(i => i.UserId == user.Id);

            ILiveBotGame game = await GetGame(stream.GameId);
            return new TwitchStream(BaseURL, ServiceType, stream, user, game);
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotUser> GetUser(string username = null, string userId = null)
        {
            User apiUser;

            if (username != null)
            {
                apiUser = await API_GetUserByLogin(username: username);
            }
            else
            {
                apiUser = await API_GetUserById(userId: userId);
            }
            return new TwitchUser(BaseURL, ServiceType, apiUser);
        }
    }
}