using LiveBot.Core.Repository.Interfaces.Monitor;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Json;
using System;
using System.Threading.Tasks;

namespace LiveBot.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(outputTemplate: "{Level:u3} {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(formatter: new JsonFormatter(), path: "logs/log-.json", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 31)
                .Enrich.FromLogContext()
                .CreateLogger();

            Log.Debug("-------------------------------------------------- Starting services... --------------------------------------------------");
            var webHost = CreateHostBuilder(args).Build();

            using (var scope = webHost.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                Log.Debug($"Starting Discord Service");
                var bot = new Discord.BotStart();
                await bot.StartAsync(services).ConfigureAwait(false);
                Log.Debug($"Started Discord Service");

                foreach (ILiveBotMonitor monitor in services.GetServices<ILiveBotMonitor>())
                {
                    Log.Debug($"Starting Monitoring Service for {monitor.ServiceType}");
                    await monitor.StartAsync().ConfigureAwait(false);
                    Log.Debug($"Started Monitoring Service for {monitor.ServiceType}");
                }

                try
                {
                    Log.Debug($"Starting MassTransit Service");
                    var bus = services.GetRequiredService<IBusControl>();
                    await bus.StartAsync().ConfigureAwait(false);
                    Log.Debug($"Started MassTransit Service");
                }
                catch (Exception e)
                {
                    Log.Error($"Error trying to start Bus:\n{e}");
                }
            }

            await webHost.RunAsync().ConfigureAwait(false);

            Log.CloseAndFlush();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}