using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiveBot.Watcher.Twitch.Contracts
{
    public class TwitchStreamOnline : IStreamOnline
    {
        public StreamSubscription Subscription { get; set; }
        public ILiveBotStream Stream { get; set; }
    }
}
