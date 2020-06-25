using GreenPipes;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Static;
using LiveBot.Discord;
using LiveBot.Discord.Consumers.Discord;
using LiveBot.Discord.Consumers.Streams;
using LiveBot.Repository;
using LiveBot.Watcher.Twitch;
using MassTransit;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

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
            // Migrate Database
            using (var context = new LiveBotDBContext())
            {
                context.Database.Migrate();
            }

            // Web services
            services.AddControllersWithViews();
            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(options =>
                {
                    options.LoginPath = "/signin";
                    options.LogoutPath = "/signout";
                })
                .AddDiscord(options =>
                {
                    options.ClientId = Environment.GetEnvironmentVariable("Discord_ClientId");
                    options.ClientSecret = Environment.GetEnvironmentVariable("Discord_ClientSecret");
                    options.Scope.Add("guilds");
                    options.SaveTokens = true;
                });

            // Add Discord Bot
            LiveBotDiscord discordBot = new LiveBotDiscord();
            services.AddSingleton(LiveBotDiscord.GetBot());
            discordBot.PopulateServices(services);

            // Add UnitOfWorkFactory
            var factory = new UnitOfWorkFactory();
            services.AddSingleton<IUnitOfWorkFactory>(factory);

            // Add Monitors
            services.AddSingleton<ILiveBotMonitor, TwitchMonitor>();

            // Add Messaging
            //services.AddScoped<StreamOnlineConsumer>();

            services.AddMassTransit(x =>
            {
                x.AddConsumer<DiscordAlertConsumer>();
                x.AddConsumer<DiscordAlertChannelConsumer>();

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
                busFactoryConfig.PrefetchCount = Queues.PrefetchCount;

                // My special events
                busFactoryConfig.ReceiveEndpoint(Queues.DiscordAlert, ep => ep.Consumer<DiscordAlertConsumer>(provider));
                busFactoryConfig.ReceiveEndpoint(Queues.DiscordAlertChannel, ep => ep.Consumer<DiscordAlertChannelConsumer>(provider));

                // Discord Events
                busFactoryConfig.ReceiveEndpoint(Queues.DiscordGuildAvailable, ep => ep.Consumer<DiscordGuildAvailableConsumer>(provider));
                busFactoryConfig.ReceiveEndpoint(Queues.DiscordGuildUpdate, ep => ep.Consumer<DiscordGuildUpdateConsumer>(provider));
                busFactoryConfig.ReceiveEndpoint(Queues.DiscordGuildDelete, ep => ep.Consumer<DiscordGuildDeleteConsumer>(provider));
                busFactoryConfig.ReceiveEndpoint(Queues.DiscordChannelUpdate, ep => ep.Consumer<DiscordChannelUpdateConsumer>(provider));
                busFactoryConfig.ReceiveEndpoint(Queues.DiscordChannelDelete, ep => ep.Consumer<DiscordChannelDeleteConsumer>(provider));
                busFactoryConfig.ReceiveEndpoint(Queues.DiscordRoleUpdate, ep => ep.Consumer<DiscordRoleUpdateConsumer>(provider));
                busFactoryConfig.ReceiveEndpoint(Queues.DiscordRoleDelete, ep => ep.Consumer<DiscordRoleDeleteConsumer>(provider));

                // Stream Events
                busFactoryConfig.ReceiveEndpoint(Queues.StreamOnlineQueueName, ep => ep.Consumer<StreamOnlineConsumer>(provider));
                busFactoryConfig.ReceiveEndpoint(Queues.StreamUpdateQueueName, ep => ep.Consumer<StreamUpdateConsumer>(provider));
                busFactoryConfig.ReceiveEndpoint(Queues.StreamOfflineQueueName, ep => ep.Consumer<StreamOfflineConsumer>(provider));
            });

            return serviceBus;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production
                // scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapControllers();
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}