using LiveBot.Watcher.Twitch;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LiveBot.Discord.SlashCommands.HealthChecks
{
    /// <summary>
    /// Fails when the Twitch token has expired without being replaced, or refreshing it keeps failing
    /// </summary>
    public class TwitchAuthHealthCheck(TwitchMonitor monitor) : IHealthCheck
    {
        // The refresh timer fires at expiry, so a token is briefly expired on every normal refresh.
        private static readonly TimeSpan ExpiryGrace = TimeSpan.FromMinutes(5);

        // Failed refreshes retry every minute, so this is roughly three minutes of failing.
        private const int MaxConsecutiveFailures = 3;

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var failures = monitor.ConsecutiveAuthFailures;
            var expiresAt = monitor.AuthExpiresAt;

            if (expiresAt is null)
                return Task.FromResult(HealthCheckResult.Unhealthy($"No auth token loaded ({failures} failed attempts)"));

            var expiredFor = DateTime.UtcNow - expiresAt.Value;
            if (expiredFor > ExpiryGrace)
                return Task.FromResult(HealthCheckResult.Unhealthy($"Auth token expired {expiredFor.TotalMinutes:F0} minutes ago ({failures} failed refreshes)"));

            if (failures >= MaxConsecutiveFailures)
                return Task.FromResult(HealthCheckResult.Unhealthy($"Auth refresh failed {failures} times in a row"));

            return Task.FromResult(HealthCheckResult.Healthy($"Auth token valid until {expiresAt.Value:O} ({failures} failed refreshes)"));
        }
    }

    /// <summary>
    /// Checks the Twitch live stream monitor. With <paramref name="requireSuccess"/> false it only asks whether the
    /// poll loop is still running, which is the part a restart can fix.
    /// </summary>
    public class TwitchMonitorHealthCheck(TwitchMonitor monitor, bool requireSuccess) : IHealthCheck
    {
        // The monitor polls every minute, so this allows four missed or failed polls before reporting.
        private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(5);

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var startedAt = monitor.MonitorStartedAt;

            // Startup retries until Twitch auth and the database are reachable, which a restart cannot speed up.
            if (startedAt is null && !requireSuccess)
                return Task.FromResult(HealthCheckResult.Degraded("Live stream monitor has not started yet"));

            if (startedAt is null || !monitor.Monitor.Enabled)
                return Task.FromResult(HealthCheckResult.Unhealthy("Live stream monitor is not running"));

            var service = monitor.Monitor;
            var now = DateTime.UtcNow;
            var last = requireSuccess ? service.LastPollSucceededAt : service.LastPollCompletedAt;
            var age = now - (last ?? startedAt.Value);
            var channels = service.ChannelsToMonitor?.Count ?? 0;
            var summary = $"{channels} channels, last poll {Describe(service.LastPollCompletedAt, now)}, last success {Describe(service.LastPollSucceededAt, now)}, {service.ConsecutivePollFailures} consecutive failures";

            if (age > StaleAfter)
            {
                var problem = requireSuccess ? "No successful poll" : "Poll loop stalled, no poll completed";
                return Task.FromResult(HealthCheckResult.Unhealthy($"{problem} in {age.TotalMinutes:F0} minutes; {summary}"));
            }

            return Task.FromResult(HealthCheckResult.Healthy(summary));
        }

        private static string Describe(DateTime? at, DateTime now) => at is null ? "never" : $"{(now - at.Value).TotalSeconds:F0}s ago";
    }
}
