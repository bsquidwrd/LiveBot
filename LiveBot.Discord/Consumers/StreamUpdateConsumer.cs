using Discord;
using LiveBot.Core.Contracts;
using MassTransit;
using Serilog;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers
{
    public class StreamUpdateConsumer : IConsumer<IStreamUpdate>
    {
        private readonly LiveBotDiscord _client;
        public StreamUpdateConsumer(LiveBotDiscord client)
        {
            _client = client;
        }

        public async Task Consume(ConsumeContext<IStreamUpdate> context)
        {
            Log.Debug("Consume for IStreamUpdate called");
            await Task.Delay(1);
        }
    }
}