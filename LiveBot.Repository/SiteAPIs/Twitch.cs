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

            List<string> lst = new List<string> { "bsquidwrd", "Lassiz" };
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
            Log.Information("OnStreamOnline run");
            foreach (var prop in e.Stream.GetType().GetProperties())
            {
                Log.Information("{0} = {1}", prop.Name, prop.GetValue(e.Stream, null));
            }
        }

        private void Monitor_OnStreamUpdate(object sender, OnStreamUpdateArgs e)
        {
            Log.Information("OnStreamUpdate run");
            // WHY THE FLYING FUCK IS THIS TRIGGERED EVERYTIME A CHECK IS RUN THROUGH THIS LIB
            // THERE'S LITERALLY NOTHING THAT'S CHANGED, YET SOMETHING IS AND I CAN'T FIGURE IT OUT
            // AHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH
            // Okay I think it's because the stream was previously live on check
            // So I think it sends this incase something has changed to process on my end
        }

        private void Monitor_OnStreamOffline(object sender, OnStreamOfflineArgs e)
        {
            Log.Information("OnStreamOffline run");
        }
    }
}