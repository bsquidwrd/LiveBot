using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Repository;

namespace LiveBot.Discord.Socket
{
    public static class LiveBotExtensions
    {
        /// <summary>
        /// Adds EnvironmentVariables to config, configures and adds
        /// <see cref="DiscordRestClient"/> and <see cref="InteractionService"/>
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static WebApplicationBuilder SetupLiveBot(this WebApplicationBuilder builder)
        {
            builder.Configuration.AddEnvironmentVariables(prefix: "LiveBot_");

            builder.Services.AddScoped<LiveBotDBContext>(_ => new LiveBotDBContext(builder.Configuration.GetValue<string>("connectionstring")));
            builder.Services.AddSingleton<IUnitOfWorkFactory>(new UnitOfWorkFactory(builder.Configuration));
            builder.Services.AddSingleton<LiveBotDiscordEventHandlers>();

            var discord = new DiscordShardedClient(new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildPresences | GatewayIntents.GuildMembers,
                SuppressUnknownDispatchWarnings = true,
            });

            builder.Services.AddSingleton(discord);
            builder.Services.AddHostedService<LiveBotService>();

            // Setup MassTransit
            builder.Services.AddLiveBotQueueing();

            return builder;
        }

        /// <summary>
        /// Registers bot Commands, maps http path for receiving Slash Commands
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static WebApplication RegisterLiveBot(this WebApplication app)
        {
            return app;
        }
    }

    public class LiveBotService : IHostedService
    {
        private readonly ILogger<LiveBotService> _logger;
        private readonly IUnitOfWorkFactory _factory;
        private readonly DiscordShardedClient _client;
        private readonly LiveBotDiscordEventHandlers _eventHandlers;
        private readonly IConfiguration _configuration;

        public LiveBotService(ILogger<LiveBotService> logger, IUnitOfWorkFactory factory, DiscordShardedClient client, LiveBotDiscordEventHandlers eventHandlers, IConfiguration configuration)
        {
            _logger = logger;
            _factory = factory;
            _client = client;
            _eventHandlers = eventHandlers;
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _factory.Migrate();

            var token = _configuration.GetValue<string>("token");
            await _client.LoginAsync(tokenType: TokenType.Bot, token: token);

            _client.ShardReady += _eventHandlers.OnReady;

            // Guild Events
            _client.GuildAvailable += _eventHandlers.GuildAvailable;
            _client.GuildUpdated += _eventHandlers.GuildUpdated;
            _client.JoinedGuild += _eventHandlers.GuildAvailable;
            _client.LeftGuild += _eventHandlers.GuildLeave;

            // Channel Events
            _client.ChannelCreated += _eventHandlers.ChannelCreated;
            _client.ChannelDestroyed += _eventHandlers.ChannelDestroyed;
            _client.ChannelUpdated += _eventHandlers.ChannelUpdated;

            // Role Events
            _client.RoleCreated += _eventHandlers.RoleCreated;
            _client.RoleDeleted += _eventHandlers.RoleDeleted;
            _client.RoleUpdated += _eventHandlers.RoleUpdated;

            // User Events
            _client.PresenceUpdated += _eventHandlers.PresenceUpdated;

            _logger.LogInformation("Starting Discord Socket");
            await _client.StartAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Discord Socket");
            return _client.StopAsync();
        }
    }
}