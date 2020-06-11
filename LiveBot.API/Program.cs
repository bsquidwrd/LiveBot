using LiveBot.Core.Repository.Interfaces.Monitor;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LiveBot.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 31)
                .CreateLogger();

            Log.Debug("-------------------------------------------------- Starting services... --------------------------------------------------");
            var webHost = CreateHostBuilder(args).Build();

            using (var scope = webHost.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                Log.Debug($"Starting Discord Service");
                var bot = new Discord.BotStart();
                await bot.StartAsync(services).ConfigureAwait(false);

                foreach (ILiveBotMonitor monitor in services.GetRequiredService<List<ILiveBotMonitor>>())
                {
                    Log.Debug($"Starting Monitoring Service for {monitor.ServiceType}");
                    ILiveBotMonitorStart monitorStart = monitor.GetStartClass();
                    await monitorStart.StartAsync(services).ConfigureAwait(false);
                }

                try
                {
                    Log.Debug($"Starting MassTransit Service");
                    var bus = services.GetRequiredService<IBusControl>();
                    await bus.StartAsync().ConfigureAwait(false);
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