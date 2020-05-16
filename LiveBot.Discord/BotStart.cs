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
            var client = services.GetRequiredService<DiscordShardedClient>();

            // Register Events
            client.ShardReady += ReadyAsync;
            client.Log += LogAsync;

            // Load Guild Information
            var loadGuildInformation = new LoadGuildInformation(services.GetRequiredService<IUnitOfWorkFactory>());
            client.GuildAvailable       += loadGuildInformation.GuildAvailable;
            client.JoinedGuild          += loadGuildInformation.GuildAvailable;
            client.LeftGuild            += loadGuildInformation.GuildLeave;
            client.ChannelCreated       += loadGuildInformation.ChannelCreated;
            client.ChannelDestroyed     += loadGuildInformation.ChannelDestroyed;
            client.ChannelUpdated       += loadGuildInformation.ChannelUpdated;
            client.RoleCreated          += loadGuildInformation.RoleCreated;
            client.RoleDeleted          += loadGuildInformation.RoleDeleted;
            client.RoleUpdated          += loadGuildInformation.RoleUpdated;

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