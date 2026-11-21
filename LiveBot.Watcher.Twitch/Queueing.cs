using LiveBot.Core.Repository.Static;
using MassTransit;

namespace LiveBot.Watcher.Twitch
{
    public static class Queueing
    {
        public static IServiceCollection AddLiveBotQueueing(this IServiceCollection services)
        {
            // Add Messaging
            services.AddMassTransit(x =>
            {
                x.AddConsumer<Consumers.TwitchStreamCheckConsumer>();
                x.AddConsumer<Consumers.TwitchStartupCatchupConsumer>();
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(Queues.QueueURL, x =>
                    {
                        x.Username(Queues.QueueUsername);
                        x.Password(Queues.QueuePassword);
                    });
                    cfg.PrefetchCount = Queues.PrefetchCount;

                    // Applies to every receive endpoint; each attempt resolves a fresh consumer scope.
                    cfg.UseMessageRetry(r =>
                    {
                        r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2));
                        r.Ignore<ArgumentNullException>();
                    });

                    // Request/Response endpoint for on-demand checks (service-specific queue)
                    cfg.ReceiveEndpoint(Queues.GetStreamCheckQueueName(LiveBot.Core.Repository.Static.ServiceEnum.Twitch), ep => ep.Consumer<Consumers.TwitchStreamCheckConsumer>(context));
                    
                    // Endpoint for startup catch-up processing
                    cfg.ReceiveEndpoint("twitch-startup-catchup", ep => ep.Consumer<Consumers.TwitchStartupCatchupConsumer>(context));
                });
            });
            return services;
        }
    }
}