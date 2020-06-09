using GreenPipes;
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

            // Add Messaging
            #region FirstMassTransit
            //services.AddMassTransit(x =>
            //{
            //    x.AddConsumer<StreamOnlineConsumer>();
            //    x.AddConsumer<StreamUpdateConsumer>();
            //    x.AddConsumer<StreamOfflineConsumer>();

            //    x.AddBus(context => Bus.Factory.CreateUsingRabbitMq(cfg =>
            //    {
            //        // configure health checks for this bus instance
            //        //cfg.UseHealthCheck(context);

            //        cfg.Host(Environment.GetEnvironmentVariable("RabbitMQURL"));

            //        cfg.ReceiveEndpoint("livebot_streamonline", ep =>
            //        {
            //            ep.PrefetchCount = 16;
            //            ep.UseMessageRetry(r => r.Interval(2, 100));

            //            ep.ConfigureConsumer<StreamOnlineConsumer>(context);
            //        });

            //        cfg.ReceiveEndpoint("livebot_streamupdate", ep =>
            //        {
            //            ep.PrefetchCount = 16;
            //            ep.UseMessageRetry(r => r.Interval(2, 100));

            //            ep.ConfigureConsumer<StreamUpdateConsumer>(context);
            //        });

            //        cfg.ReceiveEndpoint("livebot_streamoffline", ep =>
            //        {
            //            ep.PrefetchCount = 16;
            //            ep.UseMessageRetry(r => r.Interval(2, 100));

            //            ep.ConfigureConsumer<StreamOfflineConsumer>(context);
            //        });
            //    }));
            //});
            //services.AddMassTransitHostedService();
            #endregion FirstMassTransit

            services.AddMassTransit();
            services.AddSingleton(provider => Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.Host(Environment.GetEnvironmentVariable("RabbitMQURL"));

                cfg.ReceiveEndpoint("livebot_streamonline", e =>
                {
                    e.PrefetchCount = 16;
                    e.UseMessageRetry(x => x.Interval(2, 100));
                    e.Consumer<StreamOnlineConsumer>(provider);
                });

            }));
            services.AddMassTransitHostedService();
            services.AddSingleton<IBus>(provider => provider.GetRequiredService<IBusControl>());
            services.AddSingleton<IPublishEndpoint>(provider => provider.GetRequiredService<IBusControl>());
            services.AddSingleton<IHostedService, BusService>();
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