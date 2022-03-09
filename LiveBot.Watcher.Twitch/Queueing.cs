using LiveBot.Core.Repository.Static;
using MassTransit;

namespace LiveBot.Watcher.Twitch
{
    public static class Queueing
    {
        public static IServiceCollection AddLiveBotQueueing(this IServiceCollection services)
        {
            // Add Messaging
            services.AddMassTransit();
            services.AddSingleton(provider => ConfigureBus(provider));
            services.AddMassTransitHostedService();
            return services;
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
                //busFactoryConfig.UseMessageRetry(r => r.Interval(5, 5));
                busFactoryConfig.PrefetchCount = Queues.PrefetchCount;
            });

            return serviceBus;
        }
    }
}