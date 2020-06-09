using LiveBot.Core.Contracts;
using MassTransit;
using Serilog;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers
{
    public class StreamOfflineConsumer : IConsumer<IStreamOffline>
    {
        private readonly LiveBotDiscord _client;

        public StreamOfflineConsumer(LiveBotDiscord client)
        {
            _client = client;
        }

        public async Task Consume(ConsumeContext<IStreamOffline> context)
        {
            Log.Debug("Consume for IStreamOffline called");
            await Task.Delay(1);
        }
    }
}