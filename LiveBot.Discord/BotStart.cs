using Discord;
using Discord.WebSocket;
using LiveBot.Core.Repository;
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
            // You should dispose a service provider created using ASP.NET
            // when you are finished using it, at the end of your app's lifetime.
            // If you use another dependency injection framework, you should inspect
            // its documentation for the best way to do this.
            var client = services.GetRequiredService<DiscordShardedClient>();

            // The Sharded Client does not have a Ready event.
            // The ShardReady event is used instead, allowing for individual
            // control per shard.
            client.ShardReady += ReadyAsync;
            client.Log += LogAsync;

            // Load Guild Information
            var loadGuildInformation = new LoadGuildInformation(services.GetRequiredService<IUnitOfWorkFactory>());
            client.GuildAvailable += loadGuildInformation.DoGuildInfo;

            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

            // Tokens should be considered secret data, and never hard-coded.
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
            //Console.WriteLine(log.ToString());
            Log.Information(log.ToString());
            return Task.CompletedTask;
        }
    }
}