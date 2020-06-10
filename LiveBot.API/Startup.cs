using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Discord;
using LiveBot.Discord.Consumers;
using LiveBot.Repository;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
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

            // Add Discord Bot
            LiveBotDiscord discordBot = new LiveBotDiscord();
            services.AddSingleton(LiveBotDiscord.GetBot());
            discordBot.PopulateServices(services);

            // Add UnitOfWorkFactory
            var factory = new UnitOfWorkFactory();
            services.AddSingleton<IUnitOfWorkFactory>(factory);

            // Add Monitors
            List<ILiveBotMonitor> monitorList = new List<ILiveBotMonitor>
            {
                new Watcher.Twitch.TwitchMonitor()
            };
            services.AddSingleton(monitorList);

            // Add Messaging
            services.AddScoped<StreamOnlineConsumer>();

            services.AddMassTransit(x =>
            {
                x.AddConsumer<StreamOnlineConsumer>();
            });
            services.AddSingleton(provider => ConfigureBus(provider));
            services.AddMassTransitHostedService();
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

        private static IBusControl ConfigureBus(IServiceProvider provider)
        {
            var serviceBus = Bus.Factory.CreateUsingRabbitMq(busFactoryConfig =>
            {
                busFactoryConfig.Host(Environment.GetEnvironmentVariable("RabbitMQURL"));

                busFactoryConfig.ReceiveEndpoint("livebot_streamonline", ep =>
                {
                    ep.Consumer<StreamOnlineConsumer>(provider);
                });

                busFactoryConfig.ReceiveEndpoint("livebot_streamupdate", ep =>
                {
                    ep.Consumer<StreamUpdateConsumer>(provider);
                });

                busFactoryConfig.ReceiveEndpoint("livebot_streamofffline", ep =>
                {
                    ep.Consumer<StreamOfflineConsumer>(provider);
                });

            });

            return serviceBus;
        }
    }
}