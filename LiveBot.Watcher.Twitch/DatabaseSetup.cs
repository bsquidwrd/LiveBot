using LiveBot.Core.Repository.Interfaces;
using LiveBot.Repository;
using Serilog;

namespace LiveBot.Watcher.Twitch
{
    public static class DatabaseSetup
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

            builder.Services.AddSingleton<IUnitOfWorkFactory>(new UnitOfWorkFactory(builder.Configuration));

            // Setup MassTransit
            builder.Services.AddLiveBotQueueing();

            return builder;
        }
    }
}