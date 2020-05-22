using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Threading.Tasks;
using LiveBot.Watcher.Twitch;
using LiveBot.Core.Repository.Interfaces.Stream;

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

            Log.Information($@"-------------------------------------------------- Starting services... --------------------------------------------------");
            var webHost = CreateHostBuilder(args).Build();

            using (var scope = webHost.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var bot = new Discord.BotStart();

                await bot.StartAsync(services).ConfigureAwait(false);
                //var twitchMonitor =  new Twitch().StartAsync(services);
                
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