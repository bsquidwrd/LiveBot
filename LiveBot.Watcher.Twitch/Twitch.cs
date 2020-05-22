using LiveBot.Core.Repository.Interfaces.Stream;
using LiveBot.Watcher.Twitch.Interfaces;
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
    public class Twitch : ITwitch
    {
        private LiveStreamMonitorService Monitor;
        private TwitchAPI API;

        public Twitch()
        {
            Task.Run(() => StartAsync());
        }

        public async Task StartAsync()
        {
            //var bot = services.GetRequiredService<DiscordShardedBot>();
            Log.Debug("Attempting to connect with TwitchAPI and begin Monitoring process");
            ConfigLiveMonitorAsync();
            Monitor.Start();
            await Task.Delay(1);
        }

        private void ConfigLiveMonitorAsync()
        {
            API = new TwitchAPI();

            API.Settings.ClientId = "";
            API.Settings.Secret = "";
            Monitor = new LiveStreamMonitorService(api: API, checkIntervalInSeconds: 30, maxStreamRequestCountPerRequest: 100);

            List<string> channelList = new List<string> { "" };
            Monitor.SetChannelsById(channelList);

            Monitor.OnServiceStarted += Monitor_OnServiceStarted;
            Monitor.OnStreamOnline += Monitor_OnStreamOnline;
            Monitor.OnStreamOffline += Monitor_OnStreamOffline;
            //Monitor.OnStreamUpdate += Monitor_OnStreamUpdate;
        }

        // Start Events
        private void Monitor_OnServiceStarted(object sender, OnServiceStartedArgs e)
        {
            Log.Debug("Monitor service successfully connected to Twitch!");
        }

        private async void Monitor_OnStreamOnline(object sender, OnStreamOnlineArgs e)
        {
            ILiveBotStream stream = await GetStream(e.Stream);
            Log.Debug($@"OnStreamOnline: {stream.User}");
        }

        private async void Monitor_OnStreamUpdate(object sender, OnStreamUpdateArgs e)
        {
            ILiveBotStream stream = await GetStream(e.Stream);
            Log.Debug($@"OnStreamUpdate: {stream.User}");
            // WHY THE FLYING FUCK IS THIS TRIGGERED EVERYTIME A CHECK IS RUN THROUGH THIS LIB
            // THERE'S LITERALLY NOTHING THAT'S CHANGED, YET SOMETHING IS AND I CAN'T FIGURE IT OUT
            // AHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH
            // Okay I think it's because the stream was previously live on check
            // So I think it sends this incase something has changed to process on my end
        }

        private async void Monitor_OnStreamOffline(object sender, OnStreamOfflineArgs e)
        {
            ILiveBotStream stream = await GetStream(e.Stream);
            Log.Debug($@"OnStreamOffline: {stream.User}");
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
        
        // Implement Interface Requirements
        public async Task _Stop()
        {
            Log.Information("MonitorStop was called");
            await Task.Delay(1);
            Monitor.Stop();
        }

        public async Task<ILiveBotGame> GetGame(string gameId)
        {
            Game game = await API_GetGame(gameId);
            return new TwitchGame(game);
        }

        public async Task<ILiveBotStream> GetStream(Stream stream)
        {
            Game game = await API_GetGame(stream.GameId);
            ILiveBotUser liveBotUser = await GetUser(userId: stream.UserId);
            ILiveBotGame liveBotGame = new TwitchGame(game);
            return new TwitchStream(stream, liveBotUser, liveBotGame);
        }

        public async Task<ILiveBotStream> GetStream(ILiveBotUser user)
        {
            List<string> listUserId = new List<string> { user.Id };
            GetStreamsResponse streams = await API.Helix.Streams.GetStreamsAsync(userIds: listUserId);
            Stream stream = streams.Streams.FirstOrDefault(i => i.UserId == user.Id);

            ILiveBotGame game = await GetGame(stream.GameId);
            return new TwitchStream(stream, user, game);
        }

        public async Task<ILiveBotUser> GetUser(string username = null, string userId = null)
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
            return new TwitchUser(apiUser);
        }
    }
}