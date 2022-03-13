using Discord;
using Discord.Interactions;
using Discord.Rest;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Discord.SlashCommands.Helpers;
using LiveBot.Repository;
using LiveBot.Watcher.Twitch;
using System.Reflection;

namespace LiveBot.Discord.SlashCommands
{
    public static class LiveBotExtensions
    {
        /// <summary>
        /// Adds EnvironmentVariables to config, configures and adds
        /// <see cref="DiscordRestClient"/> and <see cref="InteractionService"/>
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static async Task<WebApplicationBuilder> SetupLiveBot(this WebApplicationBuilder builder)
        {
            builder.Configuration.AddEnvironmentVariables(prefix: "LiveBot_");

            builder.Services.AddSingleton<IUnitOfWorkFactory>(new UnitOfWorkFactory(builder.Configuration));

            var discord = new DiscordRestClient();
            var token = builder.Configuration.GetValue<string>("token");
            await discord.LoginAsync(TokenType.Bot, token);

            builder.Services.AddRouting();
            builder.Services.AddSingleton(discord);
            builder.Services.AddInteractionService(config =>
            {
                config.UseCompiledLambda = true;
                config.LogLevel = IsDebug() ? LogSeverity.Debug : LogSeverity.Info;
            });

            // Setup MassTransit
            builder.Services.AddLiveBotQueueing();

            // Setup Monitors
            builder.Services.AddSingleton<ILiveBotMonitor, TwitchMonitor>();

            return builder;
        }

        /// <summary>
        /// Registers bot Commands, maps http path for receiving Slash Commands
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static async Task<WebApplication> RegisterLiveBot(this WebApplication app)
        {
            app.Services.GetRequiredService<IUnitOfWorkFactory>().Migrate();

            var commands = app.Services.GetRequiredService<InteractionService>();
            commands.AddTypeConverter<Uri>(new UriConverter());
            await commands.AddModulesAsync(Assembly.GetExecutingAssembly(), app.Services);

            if (IsDebug())
            {
                var testGuildId = app.Configuration.GetValue<ulong>("testguild");
                await commands.RegisterCommandsToGuildAsync(guildId: testGuildId, deleteMissing: true);
            }
            else
            {
                await commands.RegisterCommandsGloballyAsync(deleteMissing: true);
            }

            app.MapInteractionService("/discord/interactions", app.Configuration.GetValue<string>("publickey"));

            foreach (var monitor in app.Services.GetServices<ILiveBotMonitor>())
            {
                var user = await monitor.GetUser(username: "bsquidwrd");
                app.Logger.LogInformation("Got user {username} ({userId}) for monitor {monitor}", user.Username, user.Id, monitor.ServiceType);
            }

            return app;
        }

        private static bool IsDebug()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }
}