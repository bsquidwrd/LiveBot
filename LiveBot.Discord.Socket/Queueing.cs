using LiveBot.Core.Repository.Static;
using LiveBot.Discord.Socket.Consumers.Discord;
using MassTransit;

namespace LiveBot.Discord.Socket
{
    public static class Queueing
    {
        public static IServiceCollection AddLiveBotQueueing(this IServiceCollection services)
        {
            // Add Messaging
            services.AddMassTransit(x =>
            {
                x.AddConsumer<DiscordGuildAvailableConsumer>();
                x.AddConsumer<DiscordMemberLiveConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(Queues.QueueURL, x =>
                    {
                        x.Username(Queues.QueueUsername);
                        x.Password(Queues.QueuePassword);
                    });
                    cfg.PrefetchCount = Queues.PrefetchCount;

                    // Discord Events
                    cfg.ReceiveEndpoint(Queues.DiscordGuildAvailable, ep => ep.Consumer<DiscordGuildAvailableConsumer>(context));
                    cfg.ReceiveEndpoint(Queues.DiscordMemberLive, ep => ep.Consumer<DiscordMemberLiveConsumer>(context));
                });
            });
            services.AddMassTransitHostedService();
            return services;
        }
    }
}