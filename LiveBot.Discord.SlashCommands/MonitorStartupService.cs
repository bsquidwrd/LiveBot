using LiveBot.Core.Repository.Interfaces.Monitor;

namespace LiveBot.Discord.SlashCommands
{
    /// <summary>
    /// Starts each stream monitor once the host has started, retrying until it succeeds
    /// </summary>
    public class MonitorStartupService(IEnumerable<ILiveBotMonitor> monitors, IHostApplicationLifetime lifetime, ILogger<MonitorStartupService> logger) : BackgroundService
    {
        private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Startup publishes the catch-up request, which needs the bus that MassTransit's hosted service starts.
            if (!await WaitForApplicationStartedAsync(stoppingToken))
                return;

            await Task.WhenAll(monitors.Select(monitor => StartWithRetryAsync(monitor, stoppingToken)));
        }

        private async Task<bool> WaitForApplicationStartedAsync(CancellationToken stoppingToken)
        {
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var onStarted = lifetime.ApplicationStarted.Register(() => started.TrySetResult());
            using var onStopping = stoppingToken.Register(() => started.TrySetCanceled());

            try
            {
                await started.Task;
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        // A dependency outage at startup leaves Discord running without notifications instead of stopping the process.
        private async Task StartWithRetryAsync(ILiveBotMonitor monitor, CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await monitor.StartAsync(IsWatcher: true);
                    logger.LogInformation("Started {ServiceType} monitor", monitor.ServiceType);
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unable to start {ServiceType} monitor, retrying in {RetryDelay}", monitor.ServiceType, RetryDelay);
                }

                try
                {
                    await Task.Delay(RetryDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}
