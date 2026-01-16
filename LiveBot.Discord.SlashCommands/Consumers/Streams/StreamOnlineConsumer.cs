using System.Linq.Expressions;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using LiveBot.Core.Cache;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Discord.SlashCommands.Contracts.Discord;
using LiveBot.Discord.SlashCommands.Helpers;
using MassTransit;
using StackExchange.Redis;

namespace LiveBot.Discord.SlashCommands.Consumers.Streams
{
    public class StreamOnlineConsumer : BaseStreamConsumer, IConsumer<IStreamOnline>
    {
        private static readonly double NotificationCooldownMinutes = 60;

        private readonly ConnectionMultiplexer _cache;
        private readonly ILogger<StreamOnlineConsumer> _streamOnlineLogger;

        public StreamOnlineConsumer(ILogger<StreamOnlineConsumer> logger, DiscordShardedClient client, IUnitOfWorkFactory factory, IBus bus, ConnectionMultiplexer cache)
            : base(client, factory.Create(), bus, logger)
        {
            _cache = cache;
            _streamOnlineLogger = logger;
        }

        public async Task Consume(ConsumeContext<IStreamOnline> context)
        {
            ILiveBotStream stream = context.Message.Stream;
            ILiveBotUser user = stream.User;
            ILiveBotGame game = stream.Game;

            var streamGame = await GetOrCreateStreamGame(stream, game);
            var streamUser = await _work.UserRepository.SingleOrDefaultAsync(i => i.ServiceType == stream.ServiceType && i.SourceID == user.Id);
            var streamSubscriptions = await _work.SubscriptionRepository.FindAsync(i => i.User == streamUser);

            if (!streamSubscriptions.Any())
                return;

            foreach (StreamSubscription streamSubscription in streamSubscriptions)
            {
                if (await ShouldRemoveOrphanedSubscription(streamSubscription, streamUser, stream))
                    continue;

                var (guild, channel) = await GetGuildAndChannel(streamSubscription, streamUser, stream);

                if (guild == null || channel == null)
                    continue;

                await ProcessSubscriptionNotification(stream, user, game, streamGame, streamUser, streamSubscription, guild, channel);
            }
        }

        private async Task<StreamGame> GetOrCreateStreamGame(ILiveBotStream stream, ILiveBotGame game)
        {
            Expression<Func<StreamGame, bool>> templateGamePredicate = (i => i.ServiceType == stream.ServiceType && i.SourceId == "0");
            var templateGame = await _work.GameRepository.SingleOrDefaultAsync(templateGamePredicate);

            if (game.Id == "0" || string.IsNullOrEmpty(game.Id))
            {
                if (templateGame == null)
                {
                    StreamGame newStreamGame = new StreamGame
                    {
                        ServiceType = stream.ServiceType,
                        SourceId = "0",
                        Name = "[Not Set]",
                        ThumbnailURL = ""
                    };

                    await _work.GameRepository.AddOrUpdateAsync(newStreamGame, templateGamePredicate);
                    templateGame = await _work.GameRepository.SingleOrDefaultAsync(templateGamePredicate);
                }

                return templateGame;
            }
            else
            {
                var newStreamGame = new StreamGame
                {
                    ServiceType = stream.ServiceType,
                    SourceId = game.Id,
                    Name = game.Name,
                    ThumbnailURL = game.ThumbnailURL
                };

                await _work.GameRepository.AddOrUpdateAsync(newStreamGame, i => i.ServiceType == stream.ServiceType && i.SourceId == stream.GameId);
                return await _work.GameRepository.SingleOrDefaultAsync(i => i.ServiceType == stream.ServiceType && i.SourceId == stream.GameId);
            }
        }

        private async Task<bool> ShouldRemoveOrphanedSubscription(StreamSubscription streamSubscription, StreamUser streamUser, ILiveBotStream stream)
        {
            if (streamSubscription.DiscordGuild == null || streamSubscription.DiscordChannel == null)
            {
                _logger.LogInformation("Removing orphaned Stream Subscription for {Username} on {ServiceType} - {SubscriptionId}", streamUser.Username, stream.ServiceType, streamSubscription.Id);
                await RemoveSubscriptionWithRoles(streamSubscription);
                return true;
            }

            return false;
        }

        private async Task<(SocketGuild? guild, SocketTextChannel? channel)> GetGuildAndChannel(StreamSubscription streamSubscription, StreamUser streamUser, ILiveBotStream stream)
        {
            var guild = await GetGuildSafelyAsync(streamSubscription.DiscordGuild.DiscordId);

            if (guild == null)
            {
                // Check if it's a permission issue that should trigger guild deletion
                try
                {
                    _client.GetGuild(streamSubscription.DiscordGuild.DiscordId);
                }
                catch (HttpException ex)
                {
                    if (ex.DiscordCode == DiscordErrorCode.InsufficientPermissions || ex.DiscordCode == DiscordErrorCode.MissingPermissions)
                    {
                        await _bus.Publish(new DiscordGuildDelete()
                        {
                            GuildId = streamSubscription.DiscordGuild.DiscordId,
                        });
                    }
                }
                return (null, null);
            }

            var channel = await GetChannelSafelyAsync(guild, streamSubscription.DiscordChannel.DiscordId);

            if (channel == null)
            {
                _streamOnlineLogger.LogError("Removing orphaned Stream Subscription for {Username} on {ServiceType} because channel could not be found in {GuildId} - {SubscriptionId}",
                    streamUser.Username, stream.ServiceType, guild.Id, streamSubscription.Id);
                await RemoveSubscriptionWithRoles(streamSubscription);
                return (null, null);
            }

            return (guild, channel);
        }

        private async Task RemoveSubscriptionWithRoles(StreamSubscription streamSubscription)
        {
            await RemoveSubscriptionWithRolesAsync(streamSubscription, "Subscription removed during online processing");
        }

        private async Task ProcessSubscriptionNotification(ILiveBotStream stream, ILiveBotUser user, ILiveBotGame game,
            StreamGame streamGame, StreamUser streamUser, StreamSubscription streamSubscription,
            SocketGuild guild, SocketTextChannel channel)
        {
            var discordChannel = streamSubscription.DiscordChannel;
            var discordGuild = streamSubscription.DiscordGuild;

            async Task RemoveSubscriptionAsync()
            {
                _logger.LogInformation("Removing Stream Subscription for {Username} on {ServiceType} because missing permissions in {GuildId} {ChannelId} - {SubscriptionId}",
                    streamUser.Username,
                    stream.ServiceType,
                    guild?.Id ?? 0,
                    channel?.Id ?? 0,
                    streamSubscription.Id);

                await RemoveSubscriptionWithRoles(streamSubscription);
            }

            if (channel == null)
                return;

            string notificationMessage = NotificationHelpers.GetNotificationMessage(guild: guild, stream: stream, subscription: streamSubscription, user: user, game: game);
            Embed embed = NotificationHelpers.GetStreamEmbed(stream: stream, user: user, game: game);

            var newStreamNotification = CreateStreamNotification(stream, streamGame, streamUser, discordGuild, discordChannel, streamSubscription, notificationMessage);

            var (notificationPredicate, previousNotificationPredicate) = CreateNotificationPredicates(newStreamNotification, streamUser, discordGuild, discordChannel);

            TimeSpan lockTimeout = TimeSpan.FromSeconds(30);
            string recordId = $"subscription:{stream.ServiceType}:{user.Id}:{guild.Id}";
            Guid lockGuid = Guid.NewGuid();

            bool obtainedLock = await ObtainLock(recordId, lockGuid, lockTimeout);

            try
            {
                var previousNotifications = await GetPreviousNotifications(previousNotificationPredicate, stream);
                var recentNotification = GetRecentNotification(previousNotifications, stream);

                if (ShouldUseEnhancedNotifications(discordGuild))
                {
                    await HandleEnhancedNotifications(stream, channel, previousNotifications, recentNotification, newStreamNotification, notificationMessage, embed);
                }
                else
                {
                    await HandleStandardNotifications(recentNotification, newStreamNotification);
                }

                if (recentNotification != null)
                    return;

                await SendNewNotification(newStreamNotification, notificationPredicate, channel, notificationMessage, embed,
                    streamSubscription, lockTimeout, RemoveSubscriptionAsync);
            }
            finally
            {
                if (obtainedLock)
                    await _cache.ReleaseLockAsync(recordId, identifier: lockGuid);
            }
        }

        private bool ShouldUseEnhancedNotifications(global::LiveBot.Core.Repository.Models.Discord.DiscordGuild discordGuild)
        {
            // Currently checks if the guild is in beta, but this can be easily changed
            // to check other criteria like premium status, specific guild IDs, feature flags, etc.
            return discordGuild.IsInBeta;
        }

        private async Task HandleEnhancedNotifications(ILiveBotStream stream, SocketTextChannel channel,
            List<StreamNotification> previousNotifications, StreamNotification recentNotification,
            StreamNotification newStreamNotification, string notificationMessage, Embed embed)
        {
            await HandleStaleNotifications(stream, channel, previousNotifications, recentNotification);

            if (recentNotification != null)
            {
                await HandleExistingNotificationUpdate(channel, recentNotification, newStreamNotification, notificationMessage, embed);
            }
        }

        private async Task HandleStandardNotifications(StreamNotification recentNotification, StreamNotification newStreamNotification)
        {
            if (recentNotification != null)
            {
                await HandleCooldownSuppression(recentNotification, newStreamNotification);
            }
        }

        private StreamNotification CreateStreamNotification(ILiveBotStream stream, StreamGame streamGame, StreamUser streamUser,
            global::LiveBot.Core.Repository.Models.Discord.DiscordGuild discordGuild, global::LiveBot.Core.Repository.Models.Discord.DiscordChannel discordChannel, StreamSubscription streamSubscription, string notificationMessage)
        {
            return new StreamNotification
            {
                ServiceType = stream.ServiceType,
                Success = false,
                Message = notificationMessage,
                User_SourceID = streamUser.SourceID,
                User_Username = streamUser.Username,
                User_DisplayName = streamUser.DisplayName,
                User_AvatarURL = streamUser.AvatarURL,
                User_ProfileURL = streamUser.ProfileURL,
                Stream_SourceID = stream.Id,
                Stream_Title = stream.Title,
                Stream_StartTime = stream.StartTime,
                Stream_ThumbnailURL = stream.ThumbnailURL,
                Stream_StreamURL = stream.StreamURL,
                Game_SourceID = streamGame?.SourceId,
                Game_Name = streamGame?.Name,
                Game_ThumbnailURL = streamGame?.ThumbnailURL,
                DiscordGuild_DiscordId = discordGuild.DiscordId,
                DiscordGuild_Name = discordGuild.Name,
                DiscordChannel_DiscordId = discordChannel.DiscordId,
                DiscordChannel_Name = discordChannel.Name,
                DiscordRole_DiscordId = 0,
                DiscordRole_Name = String.Join(",", streamSubscription.RolesToMention.Select(i => i.DiscordRoleId).Distinct())
            };
        }

        private (Expression<Func<StreamNotification, bool>> notificationPredicate, Expression<Func<StreamNotification, bool>> previousNotificationPredicate)
            CreateNotificationPredicates(StreamNotification newStreamNotification, StreamUser streamUser, global::LiveBot.Core.Repository.Models.Discord.DiscordGuild discordGuild, global::LiveBot.Core.Repository.Models.Discord.DiscordChannel discordChannel)
        {
            Expression<Func<StreamNotification, bool>> notificationPredicate = (i =>
                i.User_SourceID == newStreamNotification.User_SourceID &&
                i.Stream_SourceID == newStreamNotification.Stream_SourceID &&
                i.Stream_StartTime == newStreamNotification.Stream_StartTime &&
                i.DiscordGuild_DiscordId == newStreamNotification.DiscordGuild_DiscordId &&
                i.DiscordChannel_DiscordId == newStreamNotification.DiscordChannel_DiscordId &&
                i.Game_SourceID == newStreamNotification.Game_SourceID
            );

            Expression<Func<StreamNotification, bool>> previousNotificationPredicate = (i =>
                i.User_SourceID == streamUser.SourceID &&
                i.DiscordGuild_DiscordId == discordGuild.DiscordId &&
                i.DiscordChannel_DiscordId == discordChannel.DiscordId
            );

            return (notificationPredicate, previousNotificationPredicate);
        }

        private async Task<bool> ObtainLock(string recordId, Guid lockGuid, TimeSpan lockTimeout)
        {
            bool obtainedLock;
            do
            {
                obtainedLock = await _cache.ObtainLockAsync(recordId: recordId, identifier: lockGuid, expiryTime: lockTimeout);
            }
            while (!obtainedLock);

            return obtainedLock;
        }

        private async Task<List<StreamNotification>> GetPreviousNotifications(Expression<Func<StreamNotification, bool>> previousNotificationPredicate, ILiveBotStream stream)
        {
            return (await _work.NotificationRepository.FindAsync(previousNotificationPredicate))
                .Where(i => i.Success)
                .OrderByDescending(i => i.Stream_StartTime)
                .ToList();
        }

        private StreamNotification GetRecentNotification(List<StreamNotification> previousNotifications, ILiveBotStream stream)
        {
            return previousNotifications
                .FirstOrDefault(i => Math.Abs((stream.StartTime - i.Stream_StartTime).TotalMinutes) < NotificationCooldownMinutes);
        }

        private async Task HandleStaleNotifications(ILiveBotStream stream, SocketTextChannel channel,
            List<StreamNotification> previousNotifications, StreamNotification recentNotification)
        {
            var staleNotifications = previousNotifications
                .Where(i => (stream.StartTime - i.Stream_StartTime).TotalMinutes >= NotificationCooldownMinutes)
                .ToList();

            foreach (var stale in staleNotifications)
            {
                if (recentNotification != null && stale.Id == recentNotification.Id)
                    continue;

                // Only delete messages from the last 24 hours
                if ((DateTime.UtcNow - stale.Stream_StartTime).TotalHours > 24)
                {
                    continue;
                }

                bool deleted = await TryDeleteStaleMessage(channel, stale);
                await UpdateStaleNotification(stale, deleted);
            }
        }

        private async Task HandleExistingNotificationUpdate(SocketTextChannel channel, StreamNotification recentNotification,
            StreamNotification newStreamNotification, string notificationMessage, Embed embed)
        {
            if (recentNotification.DiscordMessage_DiscordId.HasValue)
            {
                await HandleExistingMessageUpdate(channel, recentNotification, newStreamNotification, notificationMessage, embed);
            }
            else
            {
                await HandleCooldownSuppression(recentNotification, newStreamNotification);
            }
        }

        private async Task HandleExistingMessageUpdate(SocketTextChannel channel, StreamNotification recentNotification,
            StreamNotification newStreamNotification, string notificationMessage, Embed embed)
        {
            var existingMessage = await TryGetExistingMessage(channel, recentNotification);

            if (existingMessage != null && existingMessage.Author.Id == _client.CurrentUser.Id)
            {
                await ProcessExistingMessageUpdate(channel, existingMessage, recentNotification, newStreamNotification, notificationMessage, embed);
            }
            else
            {
                await HandleMissingMessage(recentNotification, newStreamNotification);
            }
        }

        private async Task<IUserMessage> TryGetExistingMessage(SocketTextChannel channel, StreamNotification recentNotification)
        {
            try
            {
                var message = await GetMessageSafelyAsync(channel, recentNotification.DiscordMessage_DiscordId.Value);
                return message as IUserMessage;
            }
            catch (InsufficientPermissionsException)
            {
                return null;
            }
            catch (Exception ex)
            {
                _streamOnlineLogger.LogWarning(ex, "Unable to fetch existing notification message for {NotificationId}", recentNotification.Id);
                return null;
            }
        }

        private async Task ProcessExistingMessageUpdate(SocketTextChannel channel, IUserMessage existingMessage,
            StreamNotification recentNotification, StreamNotification newStreamNotification, string notificationMessage, Embed embed)
        {
            var currentEmbed = existingMessage.Embeds.FirstOrDefault();
            bool shouldUpdate = HasNotificationChanged(recentNotification, newStreamNotification)
                || ShouldUpdateEmbed(currentEmbed, embed)
                || !string.Equals(existingMessage.Content ?? string.Empty, notificationMessage, StringComparison.Ordinal);

            if (shouldUpdate)
            {
                await UpdateExistingMessage(channel, existingMessage, recentNotification, notificationMessage, embed);
            }

            await FinalizeNotificationUpdate(recentNotification, newStreamNotification, existingMessage);
        }

        private async Task UpdateExistingMessage(SocketTextChannel channel, IUserMessage existingMessage,
            StreamNotification recentNotification, string notificationMessage, Embed embed)
        {
            try
            {
                await channel.ModifyMessageAsync(existingMessage.Id, properties =>
                {
                    properties.Content = notificationMessage;
                    properties.Embed = embed;
                });

            }
            catch (HttpException ex)
            {
                if (ex.DiscordCode == DiscordErrorCode.MissingPermissions || ex.DiscordCode == DiscordErrorCode.InsufficientPermissions)
                {
                    await RemoveSubscriptionWithRoles(await GetSubscriptionFromNotification(recentNotification));
                    throw new InvalidOperationException("Subscription removed due to insufficient permissions", ex);
                }

                _logger.LogError(ex, "Error updating notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId}",
                    recentNotification.Id, recentNotification.ServiceType, recentNotification.User_Username,
                    recentNotification.DiscordGuild_DiscordId, recentNotification.DiscordChannel_DiscordId);
                throw;
            }
        }

        private async Task FinalizeNotificationUpdate(StreamNotification recentNotification, StreamNotification newStreamNotification, IUserMessage existingMessage)
        {
            CopyNotificationValues(recentNotification, newStreamNotification);
            recentNotification.DiscordMessage_DiscordId = existingMessage.Id;
            recentNotification.LogMessage = $"Updated at {DateTime.UtcNow:o}";
            recentNotification.Success = true;
            await _work.NotificationRepository.UpdateAsync(recentNotification);
        }

        private async Task<bool> TryDeleteStaleMessage(SocketTextChannel channel, StreamNotification stale)
        {
            if (!stale.DiscordMessage_DiscordId.HasValue)
                return true;

            try
            {
                await channel.DeleteMessageAsync(stale.DiscordMessage_DiscordId.Value);
                return true;
            }
            catch (HttpException ex)
            {
                if (ex.DiscordCode == DiscordErrorCode.UnknownMessage)
                {
                    return true;
                }
                else if (ex.DiscordCode == DiscordErrorCode.MissingPermissions || ex.DiscordCode == DiscordErrorCode.InsufficientPermissions)
                {
                    _logger.LogWarning(ex, "Missing permissions to delete stale notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId}",
                        stale.Id, stale.ServiceType, stale.User_Username, stale.DiscordGuild_DiscordId, stale.DiscordChannel_DiscordId);
                    return false;
                }
                else
                {
                    _logger.LogWarning(ex, "Failed to delete stale notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId}",
                        stale.Id, stale.ServiceType, stale.User_Username, stale.DiscordGuild_DiscordId, stale.DiscordChannel_DiscordId);
                    return false;
                }
            }
        }

        private async Task UpdateStaleNotification(StreamNotification stale, bool deleted)
        {
            if (deleted)
            {
                stale.LogMessage = $"Deleted stale notification at {DateTime.UtcNow:o}";
            }
            else
            {
                stale.LogMessage = $"Unable to delete stale notification at {DateTime.UtcNow:o}";
            }

            await _work.NotificationRepository.UpdateAsync(stale);
        }

        private async Task HandleMissingMessage(StreamNotification recentNotification, StreamNotification newStreamNotification)
        {
            recentNotification.DiscordMessage_DiscordId = null;
            CopyNotificationValues(recentNotification, newStreamNotification);
            recentNotification.Success = true;
            recentNotification.LogMessage = $"Existing notification message missing or inaccessible at {DateTime.UtcNow:o}. Cooldown active.";
            await _work.NotificationRepository.UpdateAsync(recentNotification);

            _logger.LogInformation(
                "Skipping new notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId} due to active cooldown window.",
                recentNotification.Id, recentNotification.ServiceType, recentNotification.User_Username,
                recentNotification.DiscordGuild_DiscordId, recentNotification.DiscordChannel_DiscordId);
        }

        private async Task HandleCooldownSuppression(StreamNotification recentNotification, StreamNotification newStreamNotification)
        {
            CopyNotificationValues(recentNotification, newStreamNotification);
            recentNotification.Success = true;
            recentNotification.LogMessage = $"Cooldown active at {DateTime.UtcNow:o}. Suppressing duplicate notification.";
            await _work.NotificationRepository.UpdateAsync(recentNotification);

            _logger.LogInformation(
                "Skipping new notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId} due to active cooldown window.",
                recentNotification.Id, recentNotification.ServiceType, recentNotification.User_Username,
                recentNotification.DiscordGuild_DiscordId, recentNotification.DiscordChannel_DiscordId);
        }

        private async Task SendNewNotification(StreamNotification newStreamNotification,
            Expression<Func<StreamNotification, bool>> notificationPredicate, SocketTextChannel channel,
            string notificationMessage, Embed embed, StreamSubscription streamSubscription,
            TimeSpan lockTimeout, Func<Task> removeSubscriptionAsync)
        {
            await _work.NotificationRepository.AddOrUpdateAsync(newStreamNotification, notificationPredicate);
            var streamNotification = await _work.NotificationRepository.SingleOrDefaultAsync(notificationPredicate);

            if (streamNotification.Success == true)
                return;

            var notificationDelay = CalculateNotificationDelay(streamNotification, streamSubscription);

            try
            {
                var discordMessage = await SendDiscordMessage(channel, notificationMessage, embed, lockTimeout);
                await UpdateSuccessfulNotification(streamNotification, discordMessage, notificationDelay);
            }
            catch (Exception ex)
            {
                await HandleNotificationError(streamNotification, streamSubscription, ex, notificationDelay, removeSubscriptionAsync);
            }
        }

        private double CalculateNotificationDelay(StreamNotification streamNotification, StreamSubscription streamSubscription)
        {
            var notificationDelay = (DateTime.UtcNow - streamNotification.Stream_StartTime).TotalMilliseconds;

            if (streamNotification.Stream_StartTime < streamSubscription.TimeStamp)
                notificationDelay = (DateTime.UtcNow - streamSubscription.TimeStamp).TotalMilliseconds;

            return notificationDelay;
        }

        private async Task<IUserMessage> SendDiscordMessage(SocketTextChannel channel, string notificationMessage, Embed embed, TimeSpan lockTimeout)
        {
            CancellationTokenSource cancellationToken = new();
            cancellationToken.CancelAfter((int)lockTimeout.TotalMilliseconds);

            var messageRequestOptions = new RequestOptions()
            {
                RetryMode = RetryMode.AlwaysFail,
                Timeout = (int)lockTimeout.TotalMilliseconds,
                CancelToken = cancellationToken.Token
            };

            return await channel.SendMessageAsync(text: notificationMessage, embed: embed, options: messageRequestOptions);
        }

        private async Task UpdateSuccessfulNotification(StreamNotification streamNotification, IUserMessage discordMessage, double notificationDelay)
        {
            streamNotification.DiscordMessage_DiscordId = discordMessage.Id;
            streamNotification.Success = true;
            streamNotification.LogMessage = $"Sent at {DateTime.UtcNow:o}";
            await _work.NotificationRepository.UpdateAsync(streamNotification);

            _logger.LogInformation(
                message: "Sent notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId} {@RoleIds}, {Message} {IsFromRole}, {MillisecondsToPost}",
                streamNotification.Id,
                streamNotification.ServiceType,
                streamNotification.User_Username,
                streamNotification.DiscordGuild_DiscordId.ToString(),
                streamNotification.DiscordChannel_DiscordId.ToString(),
                streamNotification.DiscordRole_Name.Split(","),
                streamNotification.Message,
                false,
                notificationDelay
            );
        }

        private async Task HandleNotificationError(StreamNotification streamNotification, StreamSubscription streamSubscription,
            Exception ex, double notificationDelay, Func<Task> removeSubscriptionAsync)
        {
            streamNotification.LogMessage = $"Error sending notification at {DateTime.UtcNow:o}: {ex.Message}";
            await _work.NotificationRepository.UpdateAsync(streamNotification);

            if (ex is HttpException httpException)
            {
                if (httpException.DiscordCode == DiscordErrorCode.MissingPermissions || httpException.DiscordCode == DiscordErrorCode.InsufficientPermissions)
                {
                    await removeSubscriptionAsync();
                    return;
                }
            }

            var roleIds = streamSubscription.RolesToMention.Select(i => i.DiscordRoleId.ToString()).Distinct().ToList();
            _logger.LogError(
                exception: ex,
                message: "Error sending notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId} {@RoleIds}, {Message} {IsFromRole}, {MillisecondsToPost}",
                streamNotification.Id,
                streamNotification.ServiceType,
                streamNotification.User_Username,
                streamNotification.DiscordGuild_DiscordId.ToString(),
                streamNotification.DiscordChannel_DiscordId.ToString(),
                roleIds,
                streamNotification.Message,
                false,
                notificationDelay
            );
        }

        private async Task<StreamSubscription> GetSubscriptionFromNotification(StreamNotification notification)
        {
            return await _work.SubscriptionRepository.SingleOrDefaultAsync(s =>
                s.DiscordGuild.DiscordId == notification.DiscordGuild_DiscordId &&
                s.DiscordChannel.DiscordId == notification.DiscordChannel_DiscordId);
        }

        private static void CopyNotificationValues(StreamNotification target, StreamNotification source)
        {
            target.ServiceType = source.ServiceType;
            target.Message = source.Message;
            target.User_SourceID = source.User_SourceID;
            target.User_Username = source.User_Username;
            target.User_DisplayName = source.User_DisplayName;
            target.User_AvatarURL = source.User_AvatarURL;
            target.User_ProfileURL = source.User_ProfileURL;
            target.Stream_SourceID = source.Stream_SourceID;
            target.Stream_Title = source.Stream_Title;
            target.Stream_StartTime = source.Stream_StartTime;
            target.Stream_ThumbnailURL = source.Stream_ThumbnailURL;
            target.Stream_StreamURL = source.Stream_StreamURL;
            target.Game_SourceID = source.Game_SourceID;
            target.Game_Name = source.Game_Name;
            target.Game_ThumbnailURL = source.Game_ThumbnailURL;
            target.DiscordGuild_DiscordId = source.DiscordGuild_DiscordId;
            target.DiscordGuild_Name = source.DiscordGuild_Name;
            target.DiscordChannel_DiscordId = source.DiscordChannel_DiscordId;
            target.DiscordChannel_Name = source.DiscordChannel_Name;
            target.DiscordRole_DiscordId = source.DiscordRole_DiscordId;
            target.DiscordRole_Name = source.DiscordRole_Name;
        }

        private static bool HasNotificationChanged(StreamNotification existing, StreamNotification updated)
        {
            return
                !string.Equals(existing.Message ?? string.Empty, updated.Message ?? string.Empty, StringComparison.Ordinal) ||
                existing.Stream_StartTime != updated.Stream_StartTime ||
                !string.Equals(existing.Stream_Title ?? string.Empty, updated.Stream_Title ?? string.Empty, StringComparison.Ordinal) ||
                !string.Equals(existing.Stream_ThumbnailURL ?? string.Empty, updated.Stream_ThumbnailURL ?? string.Empty, StringComparison.Ordinal) ||
                !string.Equals(existing.Stream_StreamURL ?? string.Empty, updated.Stream_StreamURL ?? string.Empty, StringComparison.Ordinal) ||
                !string.Equals(existing.Game_SourceID ?? string.Empty, updated.Game_SourceID ?? string.Empty, StringComparison.Ordinal) ||
                !string.Equals(existing.Game_Name ?? string.Empty, updated.Game_Name ?? string.Empty, StringComparison.Ordinal) ||
                !string.Equals(existing.Game_ThumbnailURL ?? string.Empty, updated.Game_ThumbnailURL ?? string.Empty, StringComparison.Ordinal) ||
                !string.Equals(existing.DiscordRole_Name ?? string.Empty, updated.DiscordRole_Name ?? string.Empty, StringComparison.Ordinal) ||
                !string.Equals(existing.User_DisplayName ?? string.Empty, updated.User_DisplayName ?? string.Empty, StringComparison.Ordinal) ||
                !string.Equals(existing.User_AvatarURL ?? string.Empty, updated.User_AvatarURL ?? string.Empty, StringComparison.Ordinal) ||
                !string.Equals(existing.User_ProfileURL ?? string.Empty, updated.User_ProfileURL ?? string.Empty, StringComparison.Ordinal);
        }

        private static bool ShouldUpdateEmbed(IEmbed? currentEmbed, Embed expectedEmbed)
        {
            if (currentEmbed == null)
                return true;

            if (currentEmbed.Color != expectedEmbed.Color)
                return true;

            if (!string.Equals(currentEmbed.Description ?? string.Empty, expectedEmbed.Description ?? string.Empty, StringComparison.Ordinal))
                return true;

            if (currentEmbed.Timestamp != expectedEmbed.Timestamp)
                return true;

            if (!string.Equals(currentEmbed.Url ?? string.Empty, expectedEmbed.Url ?? string.Empty, StringComparison.Ordinal))
                return true;

            var currentAuthor = currentEmbed.Author;
            var expectedAuthor = expectedEmbed.Author;

            if (!string.Equals(currentAuthor?.Name ?? string.Empty, expectedAuthor?.Name ?? string.Empty, StringComparison.Ordinal) ||
                !string.Equals(currentAuthor?.Url ?? string.Empty, expectedAuthor?.Url ?? string.Empty, StringComparison.Ordinal) ||
                !string.Equals(currentAuthor?.IconUrl ?? string.Empty, expectedAuthor?.IconUrl ?? string.Empty, StringComparison.Ordinal))
            {
                return true;
            }

            var currentFooter = currentEmbed.Footer;
            var expectedFooter = expectedEmbed.Footer;

            if (!string.Equals(currentFooter?.Text ?? string.Empty, expectedFooter?.Text ?? string.Empty, StringComparison.Ordinal))
                return true;

            if (!string.Equals(currentEmbed.Thumbnail?.Url ?? string.Empty, expectedEmbed.Thumbnail?.Url ?? string.Empty, StringComparison.Ordinal))
                return true;

            var currentFields = currentEmbed.Fields.ToList();
            var expectedFields = expectedEmbed.Fields.ToList();

            if (currentFields.Count != expectedFields.Count)
                return true;

            for (int index = 0; index < expectedFields.Count; index++)
            {
                var currentField = currentFields[index];
                var expectedField = expectedFields[index];

                if (!string.Equals(currentField.Name ?? string.Empty, expectedField.Name ?? string.Empty, StringComparison.Ordinal) ||
                    !string.Equals(currentField.Value ?? string.Empty, expectedField.Value ?? string.Empty, StringComparison.Ordinal) ||
                    currentField.Inline != expectedField.Inline)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
