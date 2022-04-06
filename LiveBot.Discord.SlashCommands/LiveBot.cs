using Discord;
using Discord.WebSocket;
using LiveBot.Core.Repository.Interfaces;

namespace LiveBot.Discord.SlashCommands
{
    public class LiveBot : IHostedService
    {
        private readonly ILogger<LiveBot> _logger;
        private readonly DiscordShardedClient _client;
        private readonly InteractionHandler _interactionHandler;
        private readonly IUnitOfWorkFactory _factory;
        private readonly IConfiguration _configuration;
        private readonly LiveBotDiscordEventHandlers _eventHandlers;
        private readonly bool IsDebug;

        public LiveBot(ILogger<LiveBot> logger, DiscordShardedClient discordSocketClient, InteractionHandler interactionHandler, IUnitOfWorkFactory factory, IConfiguration configuration, LiveBotDiscordEventHandlers eventHandlers)
        {
            _logger = logger;
            _client = discordSocketClient;
            _interactionHandler = interactionHandler;
            _factory = factory;
            _configuration = configuration;
            _eventHandlers = eventHandlers;
            IsDebug = _configuration.GetValue<bool>("IsDebug", false);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _factory.Migrate();

            _client.Log += _interactionHandler.LogAsync;
            _client.ShardReady += _eventHandlers.OnReady;

            await _interactionHandler.InitializeAsync();

            if (IsDebug)
                _logger.LogInformation("Starting bot in debug mode...");

            var token = _configuration.GetValue<string>("token");
            await _client.LoginAsync(TokenType.Bot, token);

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

            await _client.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(message: "Stopping bot...");
            await _client.StopAsync();
        }
    }
}