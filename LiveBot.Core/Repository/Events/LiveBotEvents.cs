using LiveBot.Core.Repository.Interfaces.Stream;
using System;

namespace LiveBot.Core.Repository.Events
{
    public class LiveBotEvents
    {
        public event EventHandler<ILiveBotStream> OnStreamOnline;

        public event EventHandler<ILiveBotStream> OnStreamUpdated;

        public event EventHandler<ILiveBotStream> OnStreamOffline;
    }
}