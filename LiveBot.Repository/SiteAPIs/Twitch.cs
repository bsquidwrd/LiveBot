using LiveBot.Core.Repository.Interfaces.SiteAPIs;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;

namespace LiveBot.Repository.SiteAPIs
{
    public class Twitch : ITwitch
    {
        private LiveStreamMonitorService Monitor;
        private TwitchAPI API;

        public Twitch()
        {
            Task.Run(() => ConfigLiveMonitorAsync());
        }

        private async Task ConfigLiveMonitorAsync()
        {
            API = new TwitchAPI();

            API.Settings.ClientId = "";
            API.Settings.AccessToken = "";

            Monitor = new LiveStreamMonitorService(API, 60);

            List<string> lst = new List<string> { "" };
            Monitor.SetChannelsByName(lst);

            Monitor.OnStreamOnline += Monitor_OnStreamOnline;
            Monitor.OnStreamOffline += Monitor_OnStreamOffline;
            Monitor.OnStreamUpdate += Monitor_OnStreamUpdate;

            Log.Information("Attempting to connect with TwitchAPI and begin Monitoring process");

            Monitor.Start(); //Keep at the end!

            await Task.Delay(-1);
        }

        private void Monitor_OnStreamOnline(object sender, OnStreamOnlineArgs e)
        {
            Log.Information($@"OnStreamOnline: {e.Stream.UserName} {e.Stream.Title} {e.Stream.Type.ToString()} {e.Stream.StartedAt.ToShortTimeString()}");
        }

        private void Monitor_OnStreamUpdate(object sender, OnStreamUpdateArgs e)
        {
            Log.Information($@"OnStreamUpdate: {e.Stream.UserName} {e.Stream.Title} {e.Stream.Type.ToString()} {e.Stream.StartedAt.ToShortTimeString()}");
        }

        private void Monitor_OnStreamOffline(object sender, OnStreamOfflineArgs e)
        {
            Log.Information($@"OnStreamOffline: {e.Stream.UserName} {e.Stream.Title} {e.Stream.Type.ToString()} {e.Stream.StartedAt.ToShortTimeString()}");
        }
    }
}