using Discord;
using Discord.WebSocket;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Discord.Modules;
using LiveBot.Discord.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Threading.Tasks;

namespace LiveBot.Discord
{
    // This is a minimal example of using Discord.Net's Sharded Client
    // The provided DiscordShardedClient class simplifies having multiple
    // DiscordSocketClient instances (or shards) to serve a large number of guilds.
    public class BotStart
    {
        public async Task StartAsync(IServiceProvider services)
        {
            var client = services.GetRequiredService<DiscordShardedClient>();

            // Register Events
            client.ShardReady += ReadyAsync;
            client.Log += LogAsync;

            // Load Guild Information
            var LiveBotEventHandles = new LiveBotDiscordEventHandlers(services.GetRequiredService<IUnitOfWorkFactory>());
            client.GuildAvailable += LiveBotEventHandles.GuildAvailable;
            client.JoinedGuild += LiveBotEventHandles.GuildAvailable;
            client.LeftGuild += LiveBotEventHandles.GuildLeave;
            client.ChannelCreated += LiveBotEventHandles.ChannelCreated;
            client.ChannelDestroyed += LiveBotEventHandles.ChannelDestroyed;
            client.ChannelUpdated += LiveBotEventHandles.ChannelUpdated;
            client.RoleCreated += LiveBotEventHandles.RoleCreated;
            client.RoleDeleted += LiveBotEventHandles.RoleDeleted;
            client.RoleUpdated += LiveBotEventHandles.RoleUpdated;
            client.GuildMemberUpdated += LiveBotEventHandles.GuildMemberUpdated;

            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

            // Get Bot Toke and Start the Bot
            await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DiscordToken"));
            await client.StartAsync();
        }

        private Task ReadyAsync(DiscordSocketClient shard)
        {
            Log.Information($"Shard Number {shard.ShardId} is connected and ready!");
            return Task.CompletedTask;
        }

        private Task LogAsync(LogMessage log)
        {
            Log.Information(log.ToString());
            return Task.CompletedTask;
        }
    }
}