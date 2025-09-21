using Discord;
using Discord.Net;
using Discord.WebSocket;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Streams;
using MassTransit;

namespace LiveBot.Discord.SlashCommands.Consumers.Streams
{
    /// <summary>
    /// Base class for stream consumers providing common functionality
    /// </summary>
    public abstract class BaseStreamConsumer
    {
        protected readonly DiscordShardedClient _client;
        protected readonly IUnitOfWork _work;
        protected readonly IBus _bus;
        protected readonly ILogger _logger;

        protected BaseStreamConsumer(DiscordShardedClient client, IUnitOfWork work, IBus bus, ILogger logger)
        {
            _client = client;
            _work = work;
            _bus = bus;
            _logger = logger;
        }

        /// <summary>
        /// Safely retrieves a Discord guild, handling common exceptions
        /// </summary>
        protected async Task<SocketGuild?> GetGuildSafelyAsync(ulong guildId)
        {
            try
            {
                return _client.GetGuild(guildId);
            }
            catch (HttpException ex)
            {
                if (ex.DiscordCode == DiscordErrorCode.UnknownGuild ||
                    ex.DiscordCode == DiscordErrorCode.InvalidGuild ||
                    ex.DiscordCode == DiscordErrorCode.InsufficientPermissions ||
                    ex.DiscordCode == DiscordErrorCode.MissingPermissions)
                {
                    return null;
                }
                throw;
            }
        }

        /// <summary>
        /// Safely retrieves a Discord text channel, handling common exceptions
        /// </summary>
        protected async Task<SocketTextChannel?> GetChannelSafelyAsync(SocketGuild guild, ulong channelId)
        {
            try
            {
                return guild.GetTextChannel(channelId);
            }
            catch (HttpException ex)
            {
                if (ex.DiscordCode == DiscordErrorCode.UnknownChannel ||
                    ex.DiscordCode == DiscordErrorCode.InsufficientPermissions ||
                    ex.DiscordCode == DiscordErrorCode.MissingPermissions)
                {
                    return null;
                }
                throw;
            }
        }

        /// <summary>
        /// Safely retrieves a Discord message, handling common exceptions
        /// </summary>
        protected async Task<IMessage?> GetMessageSafelyAsync(SocketTextChannel channel, ulong messageId)
        {
            try
            {
                return await channel.GetMessageAsync(messageId);
            }
            catch (HttpException ex)
            {
                if (ex.DiscordCode == DiscordErrorCode.UnknownMessage)
                {
                    return null;
                }

                if (ex.DiscordCode == DiscordErrorCode.MissingPermissions ||
                    ex.DiscordCode == DiscordErrorCode.InsufficientPermissions)
                {
                    throw new InsufficientPermissionsException("Missing permissions to retrieve message", ex);
                }

                throw;
            }
        }

        /// <summary>
        /// Safely modifies a Discord message, handling permission exceptions
        /// </summary>
        protected async Task ModifyMessageSafelyAsync(SocketTextChannel channel, ulong messageId, Action<MessageProperties> properties)
        {
            try
            {
                await channel.ModifyMessageAsync(messageId, properties);
            }
            catch (HttpException ex) when (ShouldRemoveSubscriptionForException(ex))
            {
                throw new InsufficientPermissionsException("Missing permissions to modify message", ex);
            }
        }

        /// <summary>
        /// Removes a subscription along with its associated roles
        /// </summary>
        protected async Task RemoveSubscriptionWithRolesAsync(StreamSubscription subscription, string reason = "")
        {
            var rolesToMention = await _work.RoleToMentionRepository.FindAsync(i => i.StreamSubscription == subscription);

            foreach (var roleToMention in rolesToMention)
            {
                await _work.RoleToMentionRepository.RemoveAsync(roleToMention.Id);
            }

            await _work.SubscriptionRepository.RemoveAsync(subscription.Id);

            _logger.LogInformation("Removed subscription {SubscriptionId}: {Reason}",
                subscription.Id, reason);
        }

        /// <summary>
        /// Updates a notification with error information
        /// </summary>
        protected async Task UpdateNotificationWithErrorAsync(StreamNotification notification, string errorMessage)
        {
            notification.DiscordMessage_DiscordId = null;
            notification.Success = false;
            notification.LogMessage = $"{errorMessage} at {DateTime.UtcNow:o}";
            await _work.NotificationRepository.UpdateAsync(notification);
        }

        /// <summary>
        /// Updates a notification with success information
        /// </summary>
        protected async Task UpdateNotificationWithSuccessAsync(StreamNotification notification, string successMessage)
        {
            notification.Success = true;
            notification.LogMessage = $"{successMessage} at {DateTime.UtcNow:o}";
            await _work.NotificationRepository.UpdateAsync(notification);
        }

        /// <summary>
        /// Determines if a subscription should be removed due to permission issues
        /// </summary>
        protected bool ShouldRemoveSubscriptionForException(HttpException ex)
        {
            return ex.DiscordCode == DiscordErrorCode.MissingPermissions ||
                   ex.DiscordCode == DiscordErrorCode.InsufficientPermissions;
        }

        /// <summary>
        /// Validates that a Discord user and a message are valid for bot operations
        /// </summary>
        protected bool IsValidBotMessage(IMessage? message)
        {
            return message != null &&
                   message.Author?.Id == _client.CurrentUser.Id &&
                   message is SocketUserMessage;
        }

        /// <summary>
        /// Gets the stream user from the repository
        /// </summary>
        protected async Task<StreamUser?> GetStreamUserAsync(string serviceType, string userId)
        {
            return await _work.UserRepository.SingleOrDefaultAsync(
                i => i.ServiceType.ToString() == serviceType && i.SourceID == userId);
        }

        /// <summary>
        /// Gets subscriptions for a stream user
        /// </summary>
        protected async Task<IEnumerable<StreamSubscription>> GetStreamSubscriptionsAsync(StreamUser streamUser)
        {
            return await _work.SubscriptionRepository.FindAsync(i => i.User == streamUser);
        }
    }

    /// <summary>
    /// Exception thrown when insufficient permissions are encountered
    /// </summary>
    public class InsufficientPermissionsException : Exception
    {
        public InsufficientPermissionsException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}