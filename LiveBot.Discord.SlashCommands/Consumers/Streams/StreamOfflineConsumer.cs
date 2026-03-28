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
            {
                _streamOfflineLogger.LogWarning("StreamUser not found for {Username} ({UserId}) on {ServiceType}",
                    user.Username, user.Id, stream.ServiceType);
                return;
            }

            var streamSubscriptions = await GetStreamSubscriptionsAsync(streamUser);
            if (!streamSubscriptions.Any())
            {
                _streamOfflineLogger.LogDebug("No subscriptions found for {Username} ({UserId})",
                    user.Username, user.Id);
                return;
            }

            foreach (var subscription in streamSubscriptions)
            {
                try
                {
                    await ProcessSubscriptionOffline(stream, streamUser, subscription);
                }
                catch (Exception ex)
                {
                    _streamOfflineLogger.LogError(ex, "Error processing offline subscription {SubscriptionId} for {Username}",
                        subscription.Id, user.Username);
                }
            }
        }

        private async Task ProcessSubscriptionOffline(ILiveBotStream stream, StreamUser streamUser, StreamSubscription subscription)
        {
            if (subscription.DiscordGuild == null || subscription.DiscordChannel == null)
            {
                _streamOfflineLogger.LogWarning("Subscription {SubscriptionId} has null guild or channel", subscription.Id);
                return;
            }

            var lastNotification = await GetLastNotification(subscription, stream.Id);
            if (lastNotification?.DiscordMessage_DiscordId == null)
            {
                _streamOfflineLogger.LogDebug("No last notification with message ID found for subscription {SubscriptionId}",
                    subscription.Id);
                return;
            }

            var guild = await GetGuildSafelyAsync(subscription.DiscordGuild.DiscordId);
            if (guild == null)
            {
                _streamOfflineLogger.LogWarning("Could not access guild {GuildId} for subscription {SubscriptionId}",
                    subscription.DiscordGuild.DiscordId, subscription.Id);
                return;
            }

            var channel = await GetChannelSafelyAsync(guild, subscription.DiscordChannel.DiscordId);
            if (channel == null)
            {
                _streamOfflineLogger.LogWarning("Could not access channel {ChannelId} in guild {GuildId} for subscription {SubscriptionId}",
                    subscription.DiscordChannel.DiscordId, subscription.DiscordGuild.DiscordId, subscription.Id);
                return;
            }

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
                    _streamOfflineLogger.LogWarning("Notification message {MessageId} is not a valid bot message in channel {ChannelId}",
                        lastNotification.DiscordMessage_DiscordId, channel.Id);
                    await UpdateNotificationWithErrorAsync(lastNotification,
                        "Notification message inaccessible when marking offline");
                    return;
                }

                await UpdateMessageToOffline(message!, channel, lastNotification);
            }
            catch (InsufficientPermissionsException ex)
            {
                _streamOfflineLogger.LogWarning(ex, "Insufficient permissions for guild {GuildId} channel {ChannelId}, removing subscription {SubscriptionId}",
                    subscription.DiscordGuild!.DiscordId, subscription.DiscordChannel!.DiscordId, subscription.Id);
                await HandlePermissionError(streamUser, stream, subscription);
            }
            catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.UnknownMessage)
            {
                _streamOfflineLogger.LogWarning("Message {MessageId} not found when marking offline for subscription {SubscriptionId}",
                    lastNotification.DiscordMessage_DiscordId, subscription.Id);
                await UpdateNotificationWithErrorAsync(lastNotification,
                    "Notification message missing when marking offline");
            }
            catch (Exception ex)
            {
                _streamOfflineLogger.LogError(ex, "Unexpected error processing offline message {MessageId} for subscription {SubscriptionId}",
                    lastNotification.DiscordMessage_DiscordId, subscription.Id);
                await UpdateNotificationWithErrorAsync(lastNotification,
                    $"Unexpected error when marking offline: {ex.Message}");
            }
        }

        private async Task UpdateMessageToOffline(IMessage message, SocketTextChannel channel, StreamNotification lastNotification)
        {
            try
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
            catch (Exception ex)
            {
                _streamOfflineLogger.LogError(ex, "Failed to update message {MessageId} to offline status", message.Id);
                throw;
            }
        }

        private async Task HandlePermissionError(StreamUser streamUser, ILiveBotStream stream, StreamSubscription subscription)
        {
            var reason = StreamOfflineHelper.CreateRemovalReason(
                streamUser,
                stream.ServiceType.ToString(),
                subscription.DiscordGuild!.DiscordId,
                subscription.DiscordChannel!.DiscordId,
                (int)subscription.Id);

            _streamOfflineLogger.LogInformation(reason);
            await RemoveSubscriptionWithRolesAsync(subscription, reason);
        }
    }
}
