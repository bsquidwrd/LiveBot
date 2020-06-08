﻿using LiveBot.Core.Repository.Base.Monitor;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;
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
            await _UpdateUser(stream.User);
            Log.Debug($"OnStreamOnline: {stream.User} Match: {IsValid(stream.GetStreamURL())}");
        }

        public async void Monitor_OnStreamUpdate(object sender, OnStreamUpdateArgs e)
        {
            ILiveBotStream stream = await GetStream(e.Stream);
            await _UpdateUser(stream.User);
            //Log.Debug($"OnStreamUpdate: {stream.User}");
            // WHY THE FLYING FUCK IS THIS TRIGGERED EVERYTIME A CHECK IS RUN THROUGH THIS LIB
            // THERE'S LITERALLY NOTHING THAT'S CHANGED, YET SOMETHING IS AND I CAN'T FIGURE IT OUT
            // AHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH
            // Okay I think it's because the stream was previously live on check
            // So I think it sends this incase something has changed to process on my end
        }

        public async void Monitor_OnStreamOffline(object sender, OnStreamOfflineArgs e)
        {
            ILiveBotStream stream = await GetStream(e.Stream);
            await _UpdateUser(stream.User);
            Log.Debug($"OnStreamOffline: {stream.User}");
        }

        #endregion Events

        #region API Calls

        private async Task<Game> API_GetGame(string gameId)
        {
            List<string> gameIDs = new List<string> { gameId };
            GetGamesResponse games = await API.Helix.Games.GetGamesAsync(gameIds: gameIDs).ConfigureAwait(false);
            return games.Games.FirstOrDefault(i => i.Id == gameId);
        }

        private async Task<User> API_GetUserByLogin(string username)
        {
            List<string> usernameList = new List<string> { username };
            GetUsersResponse apiUser = await API.Helix.Users.GetUsersAsync(logins: usernameList).ConfigureAwait(false);
            return apiUser.Users.FirstOrDefault(i => i.Login == username);
        }

        private async Task<User> API_GetUserById(string userId)
        {
            List<string> userIdList = new List<string> { userId };
            GetUsersResponse apiUser = await API.Helix.Users.GetUsersAsync(ids: userIdList).ConfigureAwait(false);
            return apiUser.Users.FirstOrDefault(i => i.Id == userId);
        }

        private async Task<User> API_GetUserByURL(string url)
        {
            string username = GetURLRegex(URLPattern).Match(url).Groups["username"].ToString();
            return await API_GetUserByLogin(username: username);
        }

        public async Task<ILiveBotStream> GetStream(Stream stream)
        {
            ILiveBotUser liveBotUser = await GetUser(userId: stream.UserId);
            ILiveBotGame liveBotGame = await GetGame(gameId: stream.GameId);
            return new TwitchStream(BaseURL, ServiceType, stream, liveBotUser, liveBotGame);
        }

        #endregion API Calls

        public async Task _UpdateUser(ILiveBotUser user)
        {
            StreamUser streamUser = new StreamUser()
            {
                ServiceType = ServiceType,
                SourceID = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                AvatarURL = user.AvatarURL,
                ProfileURL = user.GetProfileURL()
            };
            await _work.StreamUserRepository.AddOrUpdateAsync(streamUser, (i => i.ServiceType == ServiceType && i.SourceID == user.Id));
        }

        #region Interface Requirements

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
            if (stream == null)
                return null;
            ILiveBotGame game = await GetGame(stream.GameId);
            return new TwitchStream(BaseURL, ServiceType, stream, user, game);
        }

        /// <inheritdoc/>
        public override async Task<ILiveBotUser> GetUser(string username = null, string userId = null, string profileURL = null)
        {
            User apiUser;

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
            return new TwitchUser(BaseURL, ServiceType, apiUser);
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