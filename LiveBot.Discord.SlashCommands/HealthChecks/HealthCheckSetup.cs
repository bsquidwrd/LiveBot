using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LiveBot.Discord.SlashCommands.HealthChecks
{
    public static class HealthCheckSetup
    {
        // Liveness only covers faults inside this process, so an outage elsewhere cannot put the container in a restart loop.
        public const string LiveTag = "live";

        // MassTransit tags its bus check "ready" by default, so readiness picks it up without extra registration.
        public const string ReadyTag = "ready";

        private static readonly TimeSpan DependencyTimeout = TimeSpan.FromSeconds(5);

        public static IServiceCollection AddLiveBotHealthChecks(this IServiceCollection services, bool runWatchersInDiscord)
        {
            services.AddSingleton<DiscordGatewayState>();

            var checks = services.AddHealthChecks()
                // Readiness reports a gateway outage after two minutes; liveness waits ten so routine reconnects never trigger a restart.
                .AddTypeActivatedCheck<DiscordGatewayHealthCheck>("discord-gateway", failureStatus: null, tags: [ReadyTag], args: TimeSpan.FromMinutes(2))
                .AddTypeActivatedCheck<DiscordGatewayHealthCheck>("discord-gateway-live", failureStatus: null, tags: [LiveTag], args: TimeSpan.FromMinutes(10))
                .AddCheck<PostgresHealthCheck>("postgres", tags: [ReadyTag], timeout: DependencyTimeout)
                .AddCheck<RedisHealthCheck>("redis", tags: [ReadyTag], timeout: DependencyTimeout);

            if (runWatchersInDiscord)
            {
                checks
                    .AddCheck<TwitchAuthHealthCheck>("twitch-auth", tags: [ReadyTag])
                    .AddTypeActivatedCheck<TwitchMonitorHealthCheck>("twitch-monitor", failureStatus: null, tags: [ReadyTag], args: true)
                    .AddTypeActivatedCheck<TwitchMonitorHealthCheck>("twitch-monitor-live", failureStatus: null, tags: [LiveTag], args: false);
            }

            return services;
        }

        public static WebApplication MapLiveBotHealthChecks(this WebApplication app)
        {
            var live = new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(LiveTag),
                ResponseWriter = WriteResponse,
            };

            // MassTransit reports a broker lost after startup as Degraded, which would otherwise return 200.
            var ready = new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(ReadyTag),
                ResponseWriter = WriteResponse,
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
                },
            };

            app.MapHealthChecks("/healthz/live", live);
            app.MapHealthChecks("/healthz/ready", ready);
            // Existing container and Coolify configuration probes this path.
            app.MapHealthChecks("/healthcheck", live);

            return app;
        }

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private static Task WriteResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            var body = new
            {
                status = report.Status.ToString(),
                durationMs = Math.Round(report.TotalDuration.TotalMilliseconds),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    // A thrown exception's message can carry connection details, so only its type is shown.
                    description = e.Value.Exception is null ? e.Value.Description : $"Check threw {e.Value.Exception.GetType().Name}",
                    durationMs = Math.Round(e.Value.Duration.TotalMilliseconds),
                }),
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
        }
    }
}
