using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using LiveBot.Core.Cache;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Discord.SlashCommands.DiscordStats;
using LiveBot.Repository;
using LiveBot.Watcher.Twitch;
using Serilog;
using System.Text.RegularExpressions;

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
            var IsDebug = Convert.ToBoolean(builder.Configuration.GetValue<string>("IsDebug") ?? "false");

            string apiKey = builder.Configuration.GetValue<string>("datadogapikey") ?? "";
            string source = "csharp";
            string service = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "unknown";
            string hostname = Environment.GetEnvironmentVariable("HOSTNAME") ?? System.Net.Dns.GetHostName();
            string[] tags = new[] { IsDebug ? "Debug" : "Production" };

            builder.Services.AddCaching(instanceName: IsDebug ? "livebot:debug" : "livebot:live", configuration: builder.Configuration);

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

            var discordConfig = new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Info,
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildPresences | GatewayIntents.GuildMembers,
                SuppressUnknownDispatchWarnings = true,
                AlwaysDownloadUsers = true,
                FormatUsersInBidirectionalUnicode = true,
                DefaultRetryMode = RetryMode.AlwaysFail,
                DefaultRatelimitCallback = info =>
                {
                    if (info != null)
                    {
                        var endpoint = info.Endpoint;
                        var token = "";
                        if (endpoint.StartsWith("webhooks"))
                        {
                            var splitEndpoint = endpoint.Split('/').LastOrDefault()?.Split('?')?.FirstOrDefault();
                            token = splitEndpoint ?? "";
                        }

                        endpoint = Regex.Replace(endpoint ?? "invalid", @"\d{16,20}", ":id");
                        if (!string.IsNullOrEmpty(token))
                            endpoint = endpoint.Replace(token, ":token");
                        Console.WriteLine(endpoint);
                    }
                    return Task.CompletedTask;
                },
            };
            var discord = new DiscordShardedClient(discordConfig);

            builder.Services.AddRouting();

            // Add Discord
            //builder.Services.AddSingleton(discordConfig);
            builder.Services.AddSingleton(discord);

            // Add interaction service
            var interactionConfig = new InteractionServiceConfig()
            {
                LogLevel = LogSeverity.Info,
                UseCompiledLambda = true,
                DefaultRunMode = RunMode.Async,
            };
            builder.Services.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordShardedClient>(), interactionConfig));
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
