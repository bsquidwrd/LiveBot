using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LiveBot.Discord.SlashCommands.HealthChecks
{
    /// <summary>
    /// Remembers when each shard last dropped its gateway connection
    /// </summary>
    public class DiscordGatewayState
    {
        private readonly ConcurrentDictionary<int, DateTime> _disconnectedAt = new();
        private readonly ConcurrentDictionary<int, bool> _hasConnected = new();

        // A shard that has never connected has been down since the bot began starting it.
        public DateTime StartedAt { get; } = DateTime.UtcNow;

        public Task ShardConnected(DiscordSocketClient shard)
        {
            _hasConnected[shard.ShardId] = true;
            _disconnectedAt.TryRemove(shard.ShardId, out _);
            return Task.CompletedTask;
        }

        public Task ShardDisconnected(Exception exception, DiscordSocketClient shard)
        {
            _disconnectedAt[shard.ShardId] = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        // Discord.Net raises Disconnected only after the websocket finishes closing, so a shard can be seen
        // Disconnecting with no entry yet; its downtime starts at the first such sighting.
        public DateTime DownSince(int shardId)
        {
            if (_disconnectedAt.TryGetValue(shardId, out var at))
                return at;

            return _hasConnected.ContainsKey(shardId) ? _disconnectedAt.GetOrAdd(shardId, DateTime.UtcNow) : StartedAt;
        }

        // A check can record a sighting just after ShardConnected cleared the entry, which would otherwise date
        // the next outage from that stale sighting.
        public void ShardUp(int shardId) => _disconnectedAt.TryRemove(shardId, out _);
    }

    /// <summary>
    /// Fails once any shard has been off the gateway for longer than <paramref name="threshold"/>
    /// </summary>
    public class DiscordGatewayHealthCheck(DiscordShardedClient client, DiscordGatewayState state, TimeSpan threshold) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            // Shards are created during login and the collection is null until then.
            var shards = client.Shards;
            if (shards is null || shards.Count == 0)
            {
                var waited = now - state.StartedAt;
                return Task.FromResult(waited > threshold
                    ? HealthCheckResult.Unhealthy($"No shards started after {waited.TotalSeconds:F0}s")
                    : HealthCheckResult.Healthy($"Waiting for login ({waited.TotalSeconds:F0}s)"));
            }

            foreach (var shard in shards.Where(s => s.ConnectionState == ConnectionState.Connected))
                state.ShardUp(shard.ShardId);

            var down = shards
                .Where(s => s.ConnectionState != ConnectionState.Connected)
                .Select(s => (s.ShardId, s.ConnectionState, For: now - state.DownSince(s.ShardId)))
                .ToList();

            var connected = shards.Count - down.Count;
            var summary = $"{connected}/{shards.Count} shards connected";

            // Discord.Net reconnects on its own, so a short outage is reported without failing the check.
            var stuck = down.Where(d => d.For > threshold).ToList();
            if (stuck.Count > 0)
            {
                var detail = string.Join(", ", stuck.Select(d => $"shard {d.ShardId} {d.ConnectionState} for {d.For.TotalSeconds:F0}s"));
                return Task.FromResult(HealthCheckResult.Unhealthy($"{summary}; {detail}"));
            }

            if (down.Count > 0)
            {
                var detail = string.Join(", ", down.Select(d => $"shard {d.ShardId} {d.ConnectionState} for {d.For.TotalSeconds:F0}s"));
                return Task.FromResult(HealthCheckResult.Healthy($"{summary}; reconnecting: {detail}"));
            }

            return Task.FromResult(HealthCheckResult.Healthy(summary));
        }
    }
}
