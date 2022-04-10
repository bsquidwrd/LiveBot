using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Discord.SlashCommands.Helpers;
using System.Reflection;

namespace LiveBot.Discord.SlashCommands
{
    public class LiveBot : IHostedService
    {
        private readonly ILogger<LiveBot> _logger;
        private readonly DiscordShardedClient _client;
        private readonly InteractionService _interactionService;
        private readonly InteractionHandler _interactionHandler;
        private readonly IUnitOfWorkFactory _factory;
        private readonly IConfiguration _configuration;
        private readonly LiveBotDiscordEventHandlers _eventHandlers;
        private readonly IServiceProvider _services;
        private readonly bool IsDebug;

        public LiveBot(
            ILogger<LiveBot> logger,
            DiscordShardedClient discordClient,
            InteractionService interactionService,
            InteractionHandler interactionHandler,
            IUnitOfWorkFactory factory,
            IConfiguration configuration,
            LiveBotDiscordEventHandlers eventHandlers,
            IServiceProvider services
        )
        {
            _logger = logger;
            _client = discordClient;
            _interactionService = interactionService;
            _interactionHandler = interactionHandler;
            _factory = factory;
            _configuration = configuration;
            _eventHandlers = eventHandlers;
            _services = services;

            IsDebug = _configuration.GetValue<bool>("IsDebug", false);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _factory.Migrate();

            // Register custome TYpe Converters
            _interactionService.AddTypeConverter<Uri>(new UriConverter());

            // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            // Process the InteractionCreated payloads to execute Interactions commands
            _client.InteractionCreated += _interactionHandler.HandleInteraction;
            _interactionService.InteractionExecuted += _interactionHandler.InteractionExecuted;

            if (IsDebug)
                _logger.LogInformation("Starting bot in debug mode...");

            // Register events
            _client.Log += _interactionHandler.LogAsync;
            _client.ShardReady += _eventHandlers.OnReady; // This is everything else
            _client.ShardReady += _interactionHandler.ReadyAsync; // This handles admin command loading
            _interactionService.Log += _interactionHandler.LogAsync;

            // Guild Events
            _client.GuildAvailable += _eventHandlers.GuildAvailable;
            _client.GuildUpdated += _eventHandlers.GuildUpdated;
            _client.JoinedGuild += _eventHandlers.GuildJoined;
            _client.LeftGuild += _eventHandlers.GuildLeave;

            // Channel Events
            _client.ChannelCreated += _eventHandlers.ChannelCreated;
            _client.ChannelDestroyed += _eventHandlers.ChannelDestroyed;
            _client.ChannelUpdated += _eventHandlers.ChannelUpdated;

            // Role Events
            _client.RoleDeleted += _eventHandlers.RoleDeleted;

            // User Events
            _client.PresenceUpdated += _eventHandlers.PresenceUpdated;

            // Start the bot
            var token = _configuration.GetValue<string>("token");
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(message: "Stopping bot...");
            await _client.StopAsync();
        }
    }
}