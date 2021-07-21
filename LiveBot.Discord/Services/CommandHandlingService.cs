using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Discord.Helpers;
using LiveBot.Discord.Helpers.RuntimeResults;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LiveBot.Discord.Services
{
    public class CommandHandlingService
    {
        private readonly CommandService _commands;
        private readonly DiscordShardedClient _discord;
        private readonly IServiceProvider _services;

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _discord = services.GetRequiredService<DiscordShardedClient>();
            _services = services;

            _commands.CommandExecuted += CommandExecutedAsync;
            _commands.Log += LogAsync;
            _discord.MessageReceived += MessageReceivedAsync;

            // Add TypeReaders
            _commands.AddTypeReader(typeof(ILiveBotUser), new LiveBotUserTypeReader());
            _commands.AddTypeReader(typeof(bool), new EnhancedBoolTypeReader());
        }

        public async Task InitializeAsync()
        {
            Log.Debug("Attempting to load commands");
            await _commands.AddModulesAsync(Assembly.GetAssembly(this.GetType()), _services);
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            // Ignore system messages, or messages from other bots
            if (!(rawMessage is SocketUserMessage message))
                return;
            if (message.Source != MessageSource.User)
                return;

            if (_discord.CurrentUser == null)
                return;

            // This value holds the offset where the prefix ends
            var argPos = 0;
            bool mentionedByName = message.HasMentionPrefix(_discord.CurrentUser, ref argPos);

            // Check if mentioned by its Managed Role Some users were mentioning the role and not
            // the user This should fix that
            bool mentionedByRole = false;
            try
            {
                var channel = _discord.GetChannel(message.Channel.Id);
                if (channel is SocketTextChannel socketTextChannel)
                {
                    var guild = socketTextChannel.Guild;
                    var guildRole = guild.CurrentUser.Roles.Where(i => i.IsManaged == true).FirstOrDefault();
                    if (guildRole != null)
                        mentionedByRole = message.HasStringPrefix($"{guildRole.Mention} ", ref argPos);
                }
            }
            catch
            { }

            if (!mentionedByName && !mentionedByRole)
                return;

            // A new kind of command context, ShardedCommandContext can be utilized with the
            // commands framework
            var context = new ShardedCommandContext(_discord, message);
            await _commands.ExecuteAsync(context, argPos, _services);
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // command is unspecified when there was a search failure (command not found); we don't
            // care about these errors
            if (!command.IsSpecified)
                return;

            // the command was succesful, we don't care about this result, unless we want to log
            // that a command succeeded.
            if (result.IsSuccess)
                return;

            switch (result)
            {
                case MonitorResult monitorResult:
                    await context.Channel.SendMessageAsync(monitorResult.Reason);
                    break;

                default:
                    await context.Channel.SendMessageAsync($"error: {result.ToString()}");
                    break;
            }

            // the command failed, let's notify the user that something happened.
            //await context.Channel.SendMessageAsync($"error: {result.ToString()}");
        }

        private Task LogAsync(LogMessage log)
        {
            Log.Information(log.ToString());

            return Task.CompletedTask;
        }
    }
}