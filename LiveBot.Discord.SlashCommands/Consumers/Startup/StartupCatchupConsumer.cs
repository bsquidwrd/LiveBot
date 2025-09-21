using Discord;
using Discord.WebSocket;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Discord.SlashCommands.Consumers.Streams;
using MassTransit;

namespace LiveBot.Discord.SlashCommands.Consumers.Startup
{
    /// <summary>
    /// Consumer for handling startup catch-up operations
    /// </summary>
    public class StartupCatchupConsumer : BaseStreamConsumer, IConsumer<IStartupCatchup>
    {
        private readonly ILogger<StartupCatchupConsumer> _startupLogger;

        public StartupCatchupConsumer(
            DiscordShardedClient client,
            IUnitOfWorkFactory factory,
            IBus bus,
            ILogger<StartupCatchupConsumer> logger)
            : base(client, factory.Create(), bus, logger)
        {
            _startupLogger = logger;
        }

        public async Task Consume(ConsumeContext<IStartupCatchup> context)
        {
            var catchupRequest = context.Message;
            _startupLogger.LogInformation("Starting catch-up for {ServiceType} with {UserCount} users",
                catchupRequest.ServiceType, catchupRequest.StreamUsers.Count());

            foreach (var streamUser in catchupRequest.StreamUsers)
            {
                await ProcessUserCatchup(streamUser, catchupRequest.ServiceType);
            }

            _startupLogger.LogInformation("Completed catch-up for {ServiceType}", catchupRequest.ServiceType);
        }

        private async Task ProcessUserCatchup(ILiveBotUser streamUser, string serviceType)
        {
            try
            {
                // Get the database user
                var dbUser = await GetStreamUserAsync(serviceType, streamUser.Id);
                if (dbUser == null)
                    return;

                // Get all subscriptions for this user
                var subscriptions = await GetStreamSubscriptionsAsync(dbUser);
                if (!subscriptions.Any())
                    return;

                // For catch-up, we'll focus on marking streams as offline
                // The normal monitoring flow will handle detecting newly online streams
                // We'll check if there are recent notifications that should be marked offline

                foreach (var subscription in subscriptions)
                {
                    await ProcessSubscriptionCatchup(dbUser, subscription, serviceType);
                }
            }
            catch (Exception ex)
            {
                _startupLogger.LogError(ex, "Error processing catch-up for user {UserId} on {ServiceType}",
                    streamUser.Id, serviceType);
            }
        }

        private async Task ProcessSubscriptionCatchup(StreamUser dbUser, StreamSubscription subscription, string serviceType)
        {
            if (subscription.DiscordGuild == null || subscription.DiscordChannel == null)
                return;

            // Get recent notifications that might need to be marked offline
            var recentNotifications = await GetRecentActiveNotifications(subscription, dbUser);

            foreach (var notification in recentNotifications)
            {
                await ProcessNotificationCatchup(dbUser, subscription, notification, serviceType);
            }
        }

        private async Task<List<StreamNotification>> GetRecentActiveNotifications(StreamSubscription subscription, StreamUser streamUser)
        {
            // Find recent successful notifications that still have Discord messages
            // We'll look at notifications from the last 7 days to catch streams that might have gone offline
            var cutoffDate = DateTime.UtcNow.AddDays(-7);

            var notifications = await _work.NotificationRepository.FindAsync(i =>
                i.ServiceType == subscription.User.ServiceType &&
                i.User_SourceID == subscription.User.SourceID &&
                i.DiscordGuild_DiscordId == subscription.DiscordGuild.DiscordId &&
                i.DiscordChannel_DiscordId == subscription.DiscordChannel.DiscordId &&
                i.DiscordMessage_DiscordId != null &&
                i.Success == true &&
                i.Stream_StartTime >= cutoffDate);

            return notifications
                .OrderByDescending(i => i.Stream_StartTime)
                .ThenByDescending(i => i.TimeStamp)
                .ToList();
        }

        private async Task ProcessNotificationCatchup(StreamUser dbUser, StreamSubscription subscription,
            StreamNotification notification, string serviceType)
        {
            try
            {
                var guild = await GetGuildSafelyAsync(subscription.DiscordGuild.DiscordId);
                if (guild == null)
                    return;

                var channel = await GetChannelSafelyAsync(guild, subscription.DiscordChannel.DiscordId);
                if (channel == null)
                    return;

                var message = await GetMessageSafelyAsync(channel, (ulong)notification.DiscordMessage_DiscordId!);

                if (!IsValidBotMessage(message))
                {
                    // Message is gone or inaccessible, mark notification as failed
                    await UpdateNotificationWithErrorAsync(notification,
                        "Notification message inaccessible during startup catch-up");
                    return;
                }

                // Check if the message already shows as offline by looking at the embed
                var currentEmbed = message.Embeds.FirstOrDefault();
                if (IsAlreadyMarkedOffline(currentEmbed))
                {
                    _startupLogger.LogDebug("Message {MessageId} already marked offline for user {Username}",
                        message.Id, dbUser.Username);
                    return;
                }

                // For streams older than 8 hours, assume they're offline and mark them
                // This is a conservative approach - streams are very unlikely to be live for 8+ hours continuously
                var streamAge = DateTime.UtcNow - notification.Stream_StartTime;
                if (streamAge.TotalHours >= 8)
                {
                    await UpdateMessageToOffline(message, channel, notification, isStartupCatchup: true);

                    _startupLogger.LogInformation("Marked old stream offline during catch-up for {Username} (stream age: {Hours:F1} hours) in guild {GuildId} channel {ChannelId}",
                        dbUser.Username, streamAge.TotalHours, subscription.DiscordGuild.DiscordId, subscription.DiscordChannel.DiscordId);
                }
            }
            catch (InsufficientPermissionsException)
            {
                await HandlePermissionError(dbUser, serviceType, subscription);
            }
            catch (Exception ex)
            {
                _startupLogger.LogError(ex, "Error processing notification catch-up for {Username} in guild {GuildId} channel {ChannelId}",
                    dbUser.Username, subscription.DiscordGuild.DiscordId, subscription.DiscordChannel.DiscordId);
            }
        }

        private bool IsAlreadyMarkedOffline(IEmbed? embed)
        {
            if (embed == null)
                return false;

            // Check if there's a Status field that contains "Offline"
            var statusField = embed.Fields.FirstOrDefault(f =>
                f.Name.Equals("Status", StringComparison.InvariantCultureIgnoreCase));

            return statusField != null &&
                   statusField.Value.Contains("Offline", StringComparison.InvariantCultureIgnoreCase);
        }

        private async Task UpdateMessageToOffline(IMessage message, SocketTextChannel channel, StreamNotification notification, bool isStartupCatchup = false)
        {
            var embed = message.Embeds.FirstOrDefault();
            var embedBuilder = StreamOfflineHelper.UpdateEmbedWithOfflineStatus(embed);

            await ModifyMessageSafelyAsync(channel, message.Id, properties =>
            {
                properties.Embed = embedBuilder.Build();
            });

            var logMessage = isStartupCatchup
                ? "Marked offline during startup catch-up"
                : "Marked offline during catch-up";

            await UpdateNotificationWithSuccessAsync(notification, logMessage);
        }

        private async Task HandlePermissionError(StreamUser streamUser, string serviceType, StreamSubscription subscription)
        {
            var reason = StreamOfflineHelper.CreateRemovalReason(
                streamUser,
                serviceType,
                subscription.DiscordGuild.DiscordId,
                subscription.DiscordChannel.DiscordId,
                (int)subscription.Id);

            _startupLogger.LogInformation(reason);
            await RemoveSubscriptionWithRolesAsync(subscription, reason);
        }
    }
}