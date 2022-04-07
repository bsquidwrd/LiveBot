using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using LiveBot.Discord.SlashCommands.Modules;

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

        internal async Task LogAsync(LogMessage log)
        {
            var logLevel = GetLogLevel(log.Severity);
            _logger.Log(logLevel: logLevel, exception: log.Exception, message: "{Source} {Message}", log.Source, log.Message);
            await Task.CompletedTask;
        }

        internal async Task ReadyAsync(DiscordSocketClient client)
        {
            if (!RegisteredCommands)
            {
                try
                {
                    var adminGuildId = _configuration.GetValue<ulong>("testguild");
                    var adminGuild = _client.GetGuild(adminGuildId);
                    await _handler.AddModulesToGuildAsync(guild: adminGuild, deleteMissing: false, modules: _handler.GetModuleInfo<AdminModule>());
                    _logger.LogInformation(message: "Finished registering AdminModule with shard {ShardId}", client.ShardId);
                    RegisteredCommands = true;
                }
                catch { }
            }
        }

        internal async Task HandleInteraction(SocketInteraction interaction)
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

        internal async Task InteractionExecuted(ICommandInfo commandInfo, IInteractionContext context, DNetInteractions.IResult result)
        {
            if (!result.IsSuccess && result.Error != null)
            {
                try
                {
                    var WarningEmoji = new Emoji("\u26A0");
                    var embed = new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithTitle($"{WarningEmoji} Error!")
                        .WithDescription(result.ErrorReason)
                        .Build();

                    await context.Interaction.FollowupAsync(ephemeral: true, embed: embed);
                }
                catch { }
            }

            var logLevel = LogLevel.Information;
            if (!result.IsSuccess)
                logLevel = LogLevel.Error;

            _logger.Log(
                    logLevel: logLevel,
                    exception: (Exception?)result.GetType()?.GetProperty("Exception")?.GetValue(result, null),
                    message: "Running {ModuleName} {CommandName} for {Username} ({UserId}) in {GuildName} ({GuildId}) - {ErrorType}: {ErrorReason}",
                    commandInfo.Module.Name,
                    commandInfo.Name,
                    Format.UsernameAndDiscriminator(user: context.User, doBidirectional: true),
                    context.User.Id,
                    context.Guild.Name,
                    context.Guild.Id,
                    result.Error,
                    result.ErrorReason
                );

            await Task.CompletedTask;
        }

        internal static LogLevel GetLogLevel(LogSeverity logSeverity) =>
            logSeverity switch
            {
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Verbose => LogLevel.Debug,
                LogSeverity.Debug => LogLevel.Debug,
                _ => LogLevel.Information,
            };
    }
}