using LiveBot.Core.Repository.Interfaces.Stream;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LiveBot.Watcher.Twitch
{
    public class TwitchStart : ILiveBotMonitorStart
    {
        public async Task StartAsync (IServiceProvider services)
        {
            TwitchMonitor service = (TwitchMonitor) services.GetRequiredService<List<ILiveBotMonitor>>().Where(i => i is TwitchMonitor).First();
            service.services = services;

            service.API.Settings.ClientId = "";
            service.API.Settings.Secret = "";

            List<string> channelList = new List<string> { "" };
            service.Monitor.SetChannelsById(channelList);           

            service.Monitor.OnServiceStarted += service.Monitor_OnServiceStarted;
            service.Monitor.OnStreamOnline += service.Monitor_OnStreamOnline;
            service.Monitor.OnStreamOffline += service.Monitor_OnStreamOffline;
            //service.OnStreamUpdate += Monitor_OnStreamUpdate;

            await Task.Run(service.Monitor.Start);
        }
    }
}