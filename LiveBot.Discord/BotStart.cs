using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Interactivity;
using LiveBot.Discord.Modules;
using LiveBot.Discord.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Threading.Tasks;

namespace LiveBot.Discord
{
    /// <summary>
    /// Class that represents an instance of <c>DiscordShardedClient</c>
    /// </summary>
    public class LiveBotDiscord : DiscordShardedClient
    {
        private static readonly DiscordSocketConfig config = new DiscordSocketConfig
        {
            TotalShards = null
        };

        public static DiscordShardedClient GetBot()
        {
            return new DiscordShardedClient(config);
        }

        /// <summary>
        /// Populates the required services for the bot to run
        /// </summary>
        /// <param name="services"></param>
        public void PopulateServices(IServiceCollection services)
        {
            services.AddSingleton<InteractivityService>();
            services.AddSingleton<CommandService>();
            services.AddSingleton<CommandHandlingService>();
        }
    }

    /// <summary>
    /// Represents the starting of an instance of <c>DiscordShardedClient</c>
    /// </summary>
    public class BotStart
    {
        /// <summary>
        /// Wrapper to start the <c>DiscordShardedClient</c> instance
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public async Task StartAsync(IServiceProvider services)
        {
            var client = services.GetRequiredService<DiscordShardedClient>();

            // Register Events
            client.ShardReady += ReadyAsync;
            client.Log += LogAsync;

            // Initialize Guild Events
            var LiveBotEventHandles = new LiveBotDiscordEventHandlers(services);

            // Guild Events
            client.GuildAvailable += LiveBotEventHandles.GuildAvailable;
            //client.JoinedGuild += LiveBotEventHandles.GuildAvailable;
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

            // Get Bot Token and Start the Bot
            await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DiscordToken"));

            int recommendedShards = await client.GetRecommendedShardCountAsync().ConfigureAwait(false);
            Log.Information($"Discord recommends {recommendedShards} shards");
            if (client.Shards.Count < recommendedShards)
            {
                Log.Warning($"Discord recommends {recommendedShards} shards but there is only {client.Shards.Count}");
            }

            await client.StartAsync();
        }

        /// <summary>
        /// Discord Event Handler for when a <c>DiscordSocketClient</c> (aka a <paramref
        /// name="shard"/> in this instance) is ready
        /// </summary>
        /// <param name="shard"></param>
        /// <returns></returns>
        private Task ReadyAsync(DiscordSocketClient shard)
        {
            Log.Information($"Shard Number {shard.ShardId} is connected and ready!");
            shard.SetStatusAsync(UserStatus.Online);
            shard.SetGameAsync(name: "Your Stream!", type: ActivityType.Watching);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        private Task LogAsync(LogMessage log)
        {
            Log.Information(log.ToString());
            return Task.CompletedTask;
        }
    }
}