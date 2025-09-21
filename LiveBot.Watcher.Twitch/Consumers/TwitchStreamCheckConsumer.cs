using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces.Monitor;
using MassTransit;

namespace LiveBot.Watcher.Twitch.Consumers
{
    public class TwitchStreamCheckConsumer : IConsumer<IStreamCheckRequest>
    {
        private readonly ILogger<TwitchStreamCheckConsumer> _logger;
        private readonly TwitchMonitor _monitor;

        public TwitchStreamCheckConsumer(ILogger<TwitchStreamCheckConsumer> logger, TwitchMonitor monitor)
        {
            _logger = logger;
            _monitor = monitor;
        }

        public async Task Consume(ConsumeContext<IStreamCheckRequest> context)
        {
            try
            {
                var url = context.Message.ProfileURL;
                var user = await _monitor.GetUser(profileURL: url);
                if (user == null)
                {
                    await context.RespondAsync<IStreamCheckResponse>(new { IsLive = false, Stream = (ILiveBotStream?)null, Error = "User not found" });
                    return;
                }

                var stream = await _monitor.GetStream_Force(user);
                if (stream == null)
                {
                    await context.RespondAsync<IStreamCheckResponse>(new { IsLive = false, Stream = (ILiveBotStream?)null, Error = (string?)null });
                    return;
                }

                await context.RespondAsync<IStreamCheckResponse>(new { IsLive = true, Stream = stream, Error = (string?)null });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Twitch stream check request for {Url}", context.Message.ProfileURL);
                await context.RespondAsync<IStreamCheckResponse>(new { IsLive = false, Stream = (ILiveBotStream?)null, Error = "Internal error" });
            }
        }
    }
}