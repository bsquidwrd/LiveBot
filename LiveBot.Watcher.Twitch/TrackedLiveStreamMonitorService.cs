using TwitchLib.Api.Interfaces;
using TwitchLib.Api.Services;

namespace LiveBot.Watcher.Twitch
{
    /// <summary>
    /// A <see cref="LiveStreamMonitorService"/> that records the outcome of each poll
    /// </summary>
    public class TrackedLiveStreamMonitorService : LiveStreamMonitorService
    {
        private readonly ILogger _logger;
        private long _lastPollCompletedTicks;
        private long _lastPollSucceededTicks;
        private int _consecutivePollFailures;

        public TrackedLiveStreamMonitorService(ITwitchAPI api, ILogger logger, int checkIntervalInSeconds, int maxStreamRequestCountPerRequest)
            : base(api, checkIntervalInSeconds, maxStreamRequestCountPerRequest)
        {
            _logger = logger;
        }

        public DateTime? LastPollCompletedAt => FromTicks(Interlocked.Read(ref _lastPollCompletedTicks));
        public DateTime? LastPollSucceededAt => FromTicks(Interlocked.Read(ref _lastPollSucceededTicks));
        public int ConsecutivePollFailures => Volatile.Read(ref _consecutivePollFailures);

        // The base implementation discards every poll exception, so an expired token or a Twitch outage would
        // otherwise leave the monitor silently doing nothing. OnServiceTick is not raised because nothing subscribes to it.
        protected override async Task OnServiceTimerTick()
        {
            try
            {
                await UpdateLiveStreamersAsync();
                Interlocked.Exchange(ref _lastPollSucceededTicks, DateTime.UtcNow.Ticks);
                Interlocked.Exchange(ref _consecutivePollFailures, 0);
            }
            catch (Exception ex)
            {
                int failures = Interlocked.Increment(ref _consecutivePollFailures);
                _logger.LogWarning(exception: ex, message: "Twitch live stream poll failed ({ConsecutiveFailures} in a row)", failures);
            }
            finally
            {
                Interlocked.Exchange(ref _lastPollCompletedTicks, DateTime.UtcNow.Ticks);
            }
        }

        private static DateTime? FromTicks(long ticks) => ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
    }
}
