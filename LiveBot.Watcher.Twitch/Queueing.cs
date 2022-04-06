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
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(Queues.QueueURL, x =>
                    {
                        x.Username(Queues.QueueUsername);
                        x.Password(Queues.QueuePassword);
                    });
                    cfg.PrefetchCount = Queues.PrefetchCount;
                });
            });
            return services;
        }
    }
}