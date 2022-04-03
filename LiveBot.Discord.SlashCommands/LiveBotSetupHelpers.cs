using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Discord.SlashCommands.DiscordStats;
using LiveBot.Repository;
using LiveBot.Watcher.Twitch;
using Serilog;

namespace LiveBot.Discord.SlashCommands
{
    public static class LiveBotSetupHelpers
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

            string apiKey = builder.Configuration.GetValue<string>("datadogapikey");
            string source = "csharp";
            string service = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "unknown";
            string hostname = Environment.GetEnvironmentVariable("HOSTNAME") ?? System.Net.Dns.GetHostName();
            string[] tags = new[] { builder.Configuration.GetValue<bool>("IsDebug", false) ? "Debug" : "Production" };

            builder.Host.UseSerilog((ctx, lc) =>
                lc
                    .MinimumLevel.Information()
                    .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .WriteTo.DatadogLogs(apiKey: apiKey, source: source, service: service, host: hostname, tags: tags)
                    .Enrich.FromLogContext()
            );

            builder.Services.AddScoped<LiveBotDBContext>(_ => new LiveBotDBContext(builder.Configuration.GetValue<string>("connectionstring")));
            var unitOfWorkFactory = new UnitOfWorkFactory(builder.Configuration);
            builder.Services.AddSingleton<IUnitOfWorkFactory>(unitOfWorkFactory);
            unitOfWorkFactory.Migrate();

            var IsDebug = builder.Configuration.GetValue<bool>("IsDebug", false);
            var discordLogLevel = IsDebug ? LogSeverity.Debug : LogSeverity.Info;

            var discord = new DiscordShardedClient();
            var discordConfig = new DiscordSocketConfig()
            {
                LogLevel = discordLogLevel,
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildPresences | GatewayIntents.GuildMembers,
                SuppressUnknownDispatchWarnings = true,
                AlwaysDownloadUsers = true,
            };

            builder.Services.AddRouting();

            // Add Discord
            builder.Services.AddSingleton(discordConfig);
            builder.Services.AddSingleton(discord);

            // Add interaction service
            builder.Services.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordShardedClient>()));
            builder.Services.AddSingleton<InteractionHandler>();

            // Add my own things
            builder.Services.AddSingleton<LiveBotDiscordEventHandlers>();
            builder.Services.AddHostedService<LiveBot>();

            // Setup MassTransit
            builder.Services.AddLiveBotQueueing();

            // Add Monitors
            builder.Services.AddSingleton<ILiveBotMonitor, TwitchMonitor>();

            // Add Discord Stats
            builder.Services.AddHostedService<BotsForDiscord>();
            builder.Services.AddHostedService<BotsOnDiscord>();
            builder.Services.AddHostedService<DiscordBotList>();
            builder.Services.AddHostedService<DiscordBots>();
            builder.Services.AddHostedService<TopGG>();

            await Task.CompletedTask;
            return builder;
        }

        /// <summary>
        /// Starts the monitors
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static async Task<WebApplication> RegisterLiveBot(this WebApplication app)
        {
            foreach (var monitor in app.Services.GetServices<ILiveBotMonitor>())
            {
                await monitor.StartAsync(IsWatcher: true);
                app.Logger.LogInformation("Started {monitor} monitor", monitor.ServiceType);
            }

            return app;
        }
    }
}