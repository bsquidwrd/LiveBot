using LiveBot.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace LiveBot.Discord.SlashCommands.HealthChecks
{
    /// <summary>
    /// Opens a connection to Postgres through the pooled context factory the app uses
    /// </summary>
    public class PostgresHealthCheck(IDbContextFactory<LiveBotDBContext> factory) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var db = await factory.CreateDbContextAsync(cancellationToken);
                return await db.Database.CanConnectAsync(cancellationToken)
                    ? HealthCheckResult.Healthy("Connected")
                    : HealthCheckResult.Unhealthy("Cannot connect");
            }
            catch (Exception ex)
            {
                // Provider messages can name the host, so only the exception type is reported.
                return HealthCheckResult.Unhealthy($"Cannot connect ({ex.GetType().Name})");
            }
        }
    }

    /// <summary>
    /// Pings Redis, which holds the Twitch auth lock and the stream notification locks
    /// </summary>
    public class RedisHealthCheck(ConnectionMultiplexer redis) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (!redis.IsConnected)
                return HealthCheckResult.Unhealthy("Not connected");

            try
            {
                // PingAsync takes no token, so the registration timeout is applied here.
                var latency = await redis.GetDatabase().PingAsync().WaitAsync(cancellationToken);
                return HealthCheckResult.Healthy($"Ping {latency.TotalMilliseconds:F0}ms");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Ping failed ({ex.GetType().Name})");
            }
        }
    }
}
