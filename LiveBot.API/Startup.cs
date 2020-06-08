using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Discord;
using LiveBot.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;

namespace LiveBot.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // Add Messaging
            var messaging = new Messaging.MessagingStart();
            //services.AddSingleton(messaging);
            messaging.PopulateServices(services);

            // Add Discord Bot
            LiveBotDiscord discordBot = new LiveBotDiscord();
            services.AddSingleton(discordBot.GetBot());
            discordBot.PopulateServices(services);

            // Add UnitOfWorkFactory
            var factory = new UnitOfWorkFactory();
            services.AddSingleton<IUnitOfWorkFactory>(factory);

            // Add Monitors
            //services.AddSingleton(new Watcher.Twitch.TwitchMonitor());
            List<ILiveBotMonitor> monitorList = new List<ILiveBotMonitor>
            {
                new Watcher.Twitch.TwitchMonitor()
            };
            services.AddSingleton(monitorList);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}