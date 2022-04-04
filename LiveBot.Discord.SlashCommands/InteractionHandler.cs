using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using LiveBot.Discord.SlashCommands.Helpers;
using LiveBot.Discord.SlashCommands.Modules;
using System.Reflection;

using DNetInteractions = Discord.Interactions;

namespace LiveBot.Discord.SlashCommands
{
    public class InteractionHandler
    {
        private readonly ILogger<InteractionHandler> _logger;
        private readonly DiscordShardedClient _client;
        private readonly InteractionService _handler;
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;
        private readonly bool IsDebug;
        private bool RegisteredCommands = false;

        public InteractionHandler(ILogger<InteractionHandler> logger, DiscordShardedClient client, InteractionService handler, IServiceProvider services, IConfiguration config)
        {
            _logger = logger;
            _client = client;
            _handler = handler;
            _services = services;
            _configuration = config;
            IsDebug = _configuration.GetValue<bool>("IsDebug", false);
        }

        public async Task InitializeAsync()
        {
            // Process when the client is ready, so we can register our commands.
            _client.ShardReady += ReadyAsync;
            _handler.Log += LogAsync;

            _handler.AddTypeConverter<Uri>(new UriConverter());
            // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
            await _handler.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            // Process the InteractionCreated payloads to execute Interactions commands
            _client.InteractionCreated += HandleInteraction;
            _handler.SlashCommandExecuted += SlashCommandExecuted;
            _handler.ComponentCommandExecuted += ComponentCommandExecuted;
        }

        internal async Task LogAsync(LogMessage log)
        {
            LogLevel logLevel = log.Severity switch
            {
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Verbose => LogLevel.Debug,
                LogSeverity.Debug => LogLevel.Debug,
                _ => LogLevel.Information,
            };
            _logger.Log(logLevel: logLevel, exception: log.Exception, message: "{Source} {Message}", log.Source, log.Message);
            await Task.CompletedTask;
        }

        private async Task ReadyAsync(DiscordSocketClient client)
        {
            if (!RegisteredCommands)
            {
                try
                {
                    var adminGuildId = _configuration.GetValue<ulong>("testguild");
                    var adminGuild = _client.GetGuild(adminGuildId);
                    await _handler.AddModulesToGuildAsync(guild: adminGuild, deleteMissing: false, modules: _handler.GetModuleInfo<AdminModule>());
                    RegisteredCommands = true;
                }
                catch { }
            }
        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                if (interaction is SocketSlashCommand)
                    await interaction.DeferAsync(ephemeral: true);

                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules.
                var context = new ShardedInteractionContext(_client, interaction);

                // Execute the incoming command.
                var result = await _handler.ExecuteCommandAsync(context, _services);

                await Task.CompletedTask;
            }
            catch
            {
                // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (interaction.Type is InteractionType.ApplicationCommand)
                    await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }

        private Task SlashCommandExecuted(SlashCommandInfo info, IInteractionContext context, DNetInteractions.IResult result) =>
            HandlePostExecution(info: info, context: context, result: result);

        private Task ComponentCommandExecuted(ComponentCommandInfo info, IInteractionContext context, DNetInteractions.IResult result) =>
            HandlePostExecution(info: info, context: context, result: result);

        private async Task HandlePostExecution(ICommandInfo info, IInteractionContext context, DNetInteractions.IResult result)
        {
            if (!result.IsSuccess && result.Error != null)
            {
                _logger.LogError(
                    exception: (Exception?)result.GetType()?.GetProperty("Exception")?.GetValue(result, null),
                    message: "Error running {ModuleName} {CommandName} for {Username} ({UserId}) in {GuildName} ({GuildId}) - {ErrorType}: {ErrorReason}",
                    info.Module.Name,
                    info.Name,
                    Format.UsernameAndDiscriminator(context.User),
                    context.User.Id,
                    context.Guild.Name,
                    context.Guild.Id,
                    result.Error,
                    result.ErrorReason
                );

                var WarningEmoji = new Emoji("\u26A0");
                var embed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithTitle($"{WarningEmoji} Error!")
                    .WithDescription(result.ErrorReason)
                    .Build();

                await context.Interaction.FollowupAsync(ephemeral: true, embed: embed);
            }
            await Task.CompletedTask;
        }
    }
}