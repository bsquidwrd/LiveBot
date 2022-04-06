using LiveBot.Core.Repository.Static;
using LiveBot.Discord.SlashCommands.Consumers.Discord;
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

                x.AddConsumer<DiscordGuildAvailableConsumer>();
                x.AddConsumer<DiscordGuildUpdateConsumer>();
                x.AddConsumer<DiscordGuildDeleteConsumer>();
                x.AddConsumer<DiscordChannelUpdateConsumer>();
                x.AddConsumer<DiscordChannelDeleteConsumer>();
                x.AddConsumer<DiscordRoleUpdateConsumer>();
                x.AddConsumer<DiscordRoleDeleteConsumer>();
                x.AddConsumer<DiscordMemberLiveConsumer>();

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

                    // Discord Events
                    cfg.ReceiveEndpoint(Queues.DiscordGuildAvailable, ep => ep.Consumer<DiscordGuildAvailableConsumer>(context));
                    cfg.ReceiveEndpoint(Queues.DiscordGuildUpdate, ep => ep.Consumer<DiscordGuildUpdateConsumer>(context));
                    cfg.ReceiveEndpoint(Queues.DiscordGuildDelete, ep => ep.Consumer<DiscordGuildDeleteConsumer>(context));
                    cfg.ReceiveEndpoint(Queues.DiscordChannelUpdate, ep => ep.Consumer<DiscordChannelUpdateConsumer>(context));
                    cfg.ReceiveEndpoint(Queues.DiscordChannelDelete, ep => ep.Consumer<DiscordChannelDeleteConsumer>(context));
                    cfg.ReceiveEndpoint(Queues.DiscordRoleUpdate, ep => ep.Consumer<DiscordRoleUpdateConsumer>(context));
                    cfg.ReceiveEndpoint(Queues.DiscordRoleDelete, ep => ep.Consumer<DiscordRoleDeleteConsumer>(context));
                    cfg.ReceiveEndpoint(Queues.DiscordMemberLive, ep => ep.Consumer<DiscordMemberLiveConsumer>(context));
                });
            });
            return services;
        }
    }
}