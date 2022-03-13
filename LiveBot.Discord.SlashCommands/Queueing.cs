using LiveBot.Core.Repository.Static;
using LiveBot.Discord.SlashCommands.Consumers.Streams;
using MassTransit;

namespace LiveBot.Discord.SlashCommands
{
    public static class Queueing
    {
        public static IServiceCollection AddLiveBotQueueing(this IServiceCollection services)
        {
            // Add Messaging
            services.AddMassTransit(x =>
            {
                x.AddConsumer<StreamOnlineConsumer>();
                x.AddConsumer<StreamUpdateConsumer>();
                x.AddConsumer<StreamOfflineConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(Queues.QueueURL, x =>
                    {
                        x.Username(Queues.QueueUsername);
                        x.Password(Queues.QueuePassword);
                    });
                    cfg.PrefetchCount = Queues.PrefetchCount;

                    // Stream Events
                    cfg.ReceiveEndpoint(Queues.StreamOnlineQueueName, ep => ep.Consumer<StreamOnlineConsumer>(context));
                    cfg.ReceiveEndpoint(Queues.StreamUpdateQueueName, ep => ep.Consumer<StreamUpdateConsumer>(context));
                    cfg.ReceiveEndpoint(Queues.StreamOfflineQueueName, ep => ep.Consumer<StreamOfflineConsumer>(context));
                });
            });
            services.AddMassTransitHostedService();
            return services;
        }
    }
}