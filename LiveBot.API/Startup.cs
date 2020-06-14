using GreenPipes;
using LiveBot.Core.Consumers;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Static;
using LiveBot.Discord;
using LiveBot.Discord.Consumers.Discord;
using LiveBot.Discord.Consumers.Streams;
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
            //services.AddScoped<StreamOnlineConsumer>();

            services.AddMassTransit(x =>
            {
                x.AddConsumer<MonitorRefreshAuthConsumer>();
                x.AddConsumer<MonitorUpdateUsersConsumer>();
                
                x.AddConsumer<DiscordGuildAvailableConsumer>();
                x.AddConsumer<DiscordGuildUpdateConsumer>();
                x.AddConsumer<DiscordGuildDeleteConsumer>();
                x.AddConsumer<DiscordChannelUpdateConsumer>();
                x.AddConsumer<DiscordChannelDeleteConsumer>();
                x.AddConsumer<DiscordRoleUpdateConsumer>();
                x.AddConsumer<DiscordRoleDeleteConsumer>();

                x.AddConsumer<StreamOnlineConsumer>();
                x.AddConsumer<StreamUpdateConsumer>();
                x.AddConsumer<StreamOfflineConsumer>();
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
                busFactoryConfig.Host(Queues.QueueURL, x =>
                {
                    x.Username(Queues.QueueUsername);
                    x.Password(Queues.QueuePassword);
                });
                busFactoryConfig.UseMessageRetry(r => r.Interval(5, 5));

                // Base Monitor Consumers
                busFactoryConfig.ReceiveEndpoint(Queues.RefreshAuthQueueName, ep =>
                {
                    ep.Consumer<MonitorRefreshAuthConsumer>(provider);
                    ep.PrefetchCount = Queues.PrefetchCount;
                });
                busFactoryConfig.ReceiveEndpoint(Queues.UpdateUsersQueueName, ep =>
                {
                    ep.Consumer<MonitorUpdateUsersConsumer>(provider);
                    ep.PrefetchCount = Queues.PrefetchCount;
                });

                // Discord Events
                busFactoryConfig.ReceiveEndpoint(Queues.DiscordGuildAvailable, ep =>
                {
                    ep.Consumer<DiscordGuildAvailableConsumer>(provider);
                    ep.PrefetchCount = Queues.PrefetchCount;
                });
                busFactoryConfig.ReceiveEndpoint(Queues.DiscordGuildUpdate, ep =>
                {
                    ep.Consumer<DiscordGuildUpdateConsumer>(provider);
                    ep.PrefetchCount = Queues.PrefetchCount;
                });
                busFactoryConfig.ReceiveEndpoint(Queues.DiscordGuildDelete, ep =>
                {
                    ep.Consumer<DiscordGuildDeleteConsumer>(provider);
                    ep.PrefetchCount = Queues.PrefetchCount;
                });
                busFactoryConfig.ReceiveEndpoint(Queues.DiscordChannelUpdate, ep =>
                {
                    ep.Consumer<DiscordChannelUpdateConsumer>(provider);
                    ep.PrefetchCount = Queues.PrefetchCount;
                });
                busFactoryConfig.ReceiveEndpoint(Queues.DiscordChannelDelete, ep =>
                {
                    ep.Consumer<DiscordChannelDeleteConsumer>(provider);
                    ep.PrefetchCount = Queues.PrefetchCount;
                });
                busFactoryConfig.ReceiveEndpoint(Queues.DiscordRoleUpdate, ep =>
                {
                    ep.Consumer<DiscordRoleUpdateConsumer>(provider);
                    ep.PrefetchCount = Queues.PrefetchCount;
                });
                busFactoryConfig.ReceiveEndpoint(Queues.DiscordRoleDelete, ep =>
                {
                    ep.Consumer<DiscordRoleDeleteConsumer>(provider);
                    ep.PrefetchCount = Queues.PrefetchCount;
                });

                // Stream Events
                busFactoryConfig.ReceiveEndpoint(Queues.StreamOnlineQueueName, ep =>
                {
                    ep.Consumer<StreamOnlineConsumer>(provider);
                    ep.PrefetchCount = Queues.PrefetchCount;
                });
                busFactoryConfig.ReceiveEndpoint(Queues.StreamUpdateQueueName, ep =>
                {
                    ep.Consumer<StreamUpdateConsumer>(provider);
                    ep.PrefetchCount = Queues.PrefetchCount;
                }
                );
                busFactoryConfig.ReceiveEndpoint(Queues.StreamOfflineQueueName, ep =>
                {
                    ep.Consumer<StreamOfflineConsumer>(provider);
                    ep.PrefetchCount = Queues.PrefetchCount;
                });
            });

            return serviceBus;
        }
    }
}