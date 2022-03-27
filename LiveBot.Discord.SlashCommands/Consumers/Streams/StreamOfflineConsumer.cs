using Discord.Rest;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using MassTransit;

namespace LiveBot.Discord.SlashCommands.Consumers.Streams
{
    public class StreamOfflineConsumer : IConsumer<IStreamOffline>
    {
        private readonly DiscordRestClient _client;
        private readonly IUnitOfWork _work;
        private readonly IBusControl _bus;
        private readonly ILogger<StreamOfflineConsumer> _logger;

        public StreamOfflineConsumer(DiscordRestClient client, IUnitOfWorkFactory factory, IBusControl bus, ILogger<StreamOfflineConsumer> logger)
        {
            _client = client;
            _work = factory.Create();
            _bus = bus;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IStreamOffline> context)
        {
            try
            {
                // TODO: Implement StreamOffline.Consume
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Unable to process Stream Offline event");
            }
        }
    }
}