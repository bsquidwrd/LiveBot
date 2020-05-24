using LiveBot.Core.Repository.Interfaces.Stream;
using System.Collections.Generic;
using System.Linq;

namespace LiveBot.Discord.Services.LiveBot
{
    public class DetermineStreamChannel
    {
        private readonly string StreamURL;
        private readonly List<ILiveBotMonitor> _monitors;

        public DetermineStreamChannel(List<ILiveBotMonitor> monitors, string StreamURL)
        {
            _monitors = monitors;
            this.StreamURL = StreamURL;
        }

        public ILiveBotMonitor Check()
        {
            return _monitors.Where(m => m.IsValid(StreamURL)).FirstOrDefault();
        }
    }
}