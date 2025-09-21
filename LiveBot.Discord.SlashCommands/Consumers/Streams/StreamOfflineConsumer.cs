using Discord;
using Discord.Net;
using Discord.WebSocket;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using MassTransit;

namespace LiveBot.Discord.SlashCommands.Consumers.Streams
{
    public class StreamOfflineConsumer : BaseStreamConsumer, IConsumer<IStreamOffline>
    {
        private readonly ILogger<StreamOfflineConsumer> _streamOfflineLogger;

        public StreamOfflineConsumer(
            DiscordShardedClient client,
            IUnitOfWorkFactory factory,
            IBus bus,
            ILogger<StreamOfflineConsumer> logger)
            : base(client, factory.Create(), bus, logger)
        {
            _streamOfflineLogger = logger;
        }

        public async Task Consume(ConsumeContext<IStreamOffline> context)
        {
            var stream = context.Message.Stream;
            var user = stream.User;

            var streamUser = await GetStreamUserAsync(stream.ServiceType, user.Id);
            if (streamUser == null)
                return;

            var streamSubscriptions = await GetStreamSubscriptionsAsync(streamUser);
            if (!streamSubscriptions.Any())
                return;

            foreach (var subscription in streamSubscriptions)
            {
                await ProcessSubscriptionOffline(stream, streamUser, subscription);
            }
        }

        private async Task ProcessSubscriptionOffline(ILiveBotStream stream, StreamUser streamUser, StreamSubscription subscription)
        {
            if (subscription.DiscordGuild == null || subscription.DiscordChannel == null)
                return;

            var lastNotification = await GetLastNotification(subscription, stream.Id);
            if (lastNotification?.DiscordMessage_DiscordId == null)
                return;

            var guild = await GetGuildSafelyAsync(subscription.DiscordGuild.DiscordId);
            if (guild == null)
                return;

            var channel = await GetChannelSafelyAsync(guild, subscription.DiscordChannel.DiscordId);
            if (channel == null)
                return;

            await ProcessOfflineMessage(stream, streamUser, subscription, lastNotification, channel);
        }

        private async Task<StreamNotification?> GetLastNotification(StreamSubscription subscription, string streamId)
        {
            var predicate = StreamOfflineHelper.CreatePreviousNotificationPredicate(subscription, subscription.User, streamId);
            return await _work.NotificationRepository.SingleOrDefaultAsync(predicate);
        }

        private async Task ProcessOfflineMessage(
            ILiveBotStream stream,
            StreamUser streamUser,
            StreamSubscription subscription,
            StreamNotification lastNotification,
            SocketTextChannel channel)
        {
            try
            {
                var message = await GetMessageSafelyAsync(channel, (ulong)lastNotification.DiscordMessage_DiscordId!);

                if (!IsValidBotMessage(message))
                {
                    await UpdateNotificationWithErrorAsync(lastNotification,
                        "Notification message inaccessible when marking offline");
                    return;
                }

                await UpdateMessageToOffline(message, channel, lastNotification);
            }
            catch (InsufficientPermissionsException)
            {
                await HandlePermissionError(streamUser, stream, subscription);
            }
            catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.UnknownMessage)
            {
                await UpdateNotificationWithErrorAsync(lastNotification,
                    "Notification message missing when marking offline");
            }
        }

        private async Task UpdateMessageToOffline(IMessage message, SocketTextChannel channel, StreamNotification lastNotification)
        {
            var embed = message.Embeds.FirstOrDefault();
            var embedBuilder = StreamOfflineHelper.UpdateEmbedWithOfflineStatus(embed);

            await ModifyMessageSafelyAsync(channel, message.Id, properties =>
            {
                properties.Embed = embedBuilder.Build();
            });

            await UpdateNotificationWithSuccessAsync(lastNotification,
                StreamOfflineHelper.CreateOfflineLogMessage().Replace($" at {DateTime.UtcNow:o}", ""));
        }

        private async Task HandlePermissionError(StreamUser streamUser, ILiveBotStream stream, StreamSubscription subscription)
        {
            var reason = StreamOfflineHelper.CreateRemovalReason(
                streamUser,
                stream.ServiceType.ToString(),
                subscription.DiscordGuild.DiscordId,
                subscription.DiscordChannel.DiscordId,
                (int)subscription.Id);

            _streamOfflineLogger.LogInformation(reason);
            await RemoveSubscriptionWithRolesAsync(subscription, reason);
        }
    }
}