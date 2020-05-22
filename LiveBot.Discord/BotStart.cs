using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
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
    public class LiveBotDiscord : DiscordShardedClient
    {
        public DiscordShardedClient GetBot()
        {
            var config = new DiscordSocketConfig
            {
                TotalShards = 1
            };
            return new DiscordShardedClient(config);
        }

        public void PopulateServices(IServiceCollection services)
        {
            services.AddSingleton<InteractiveService>();
            services.AddSingleton<CommandService>();
            services.AddSingleton<CommandHandlingService>();
        }
    }

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

            // Guild Events
            client.GuildAvailable += LiveBotEventHandles.GuildAvailable;
            client.GuildUpdated += LiveBotEventHandles.GuildUpdated;
            client.JoinedGuild += LiveBotEventHandles.GuildAvailable;
            client.LeftGuild += LiveBotEventHandles.GuildLeave;

            // Channel Events
            client.ChannelCreated += LiveBotEventHandles.ChannelCreated;
            client.ChannelDestroyed += LiveBotEventHandles.ChannelDestroyed;
            client.ChannelUpdated += LiveBotEventHandles.ChannelUpdated;

            // Role Events
            client.RoleCreated += LiveBotEventHandles.RoleCreated;
            client.RoleDeleted += LiveBotEventHandles.RoleDeleted;
            client.RoleUpdated += LiveBotEventHandles.RoleUpdated;

            // User Events
            client.GuildMemberUpdated += LiveBotEventHandles.GuildMemberUpdated;

            // Populate Commands
            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

            // Get Bot Toke and Start the Bot
            await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DiscordToken"));
            await client.StartAsync();
        }

        private Task ReadyAsync(DiscordSocketClient shard)
        {
            Log.Information($"Shard Number {shard.ShardId} is connected and ready!");
            shard.SetGameAsync(name: $@"@{shard.CurrentUser.Username} help", type: ActivityType.Playing);
            return Task.CompletedTask;
        }

        private Task LogAsync(LogMessage log)
        {
            Log.Information(log.ToString());
            return Task.CompletedTask;
        }
    }
}