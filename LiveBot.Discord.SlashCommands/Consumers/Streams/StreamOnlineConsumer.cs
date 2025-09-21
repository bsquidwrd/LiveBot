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
    public class StreamOnlineConsumer : IConsumer<IStreamOnline>
    {
        private static readonly double NotificationCooldownMinutes = 60;

        private readonly IBus _bus;
        private readonly ConnectionMultiplexer _cache;
        private readonly DiscordShardedClient _client;
        private readonly ILogger<StreamOnlineConsumer> _logger;
        private readonly IUnitOfWork _work;

        public StreamOnlineConsumer(ILogger<StreamOnlineConsumer> logger, DiscordShardedClient client, IUnitOfWorkFactory factory, IBus bus, ConnectionMultiplexer cache)
        {
            _logger = logger;
            _client = client;
            _work = factory.Create();
            _bus = bus;
            _cache = cache;
        }

        public async Task Consume(ConsumeContext<IStreamOnline> context)
        {
            ILiveBotStream stream = context.Message.Stream;
            ILiveBotUser user = stream.User;
            ILiveBotGame game = stream.Game;
            Expression<Func<StreamGame, bool>> templateGamePredicate = (i => i.ServiceType == stream.ServiceType && i.SourceId == "0");
            var templateGame = await _work.GameRepository.SingleOrDefaultAsync(templateGamePredicate);
            var streamUser = await _work.UserRepository.SingleOrDefaultAsync(i => i.ServiceType == stream.ServiceType && i.SourceID == user.Id);
            var streamSubscriptions = await _work.SubscriptionRepository.FindAsync(i => i.User == streamUser);
            StreamGame streamGame;

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

                streamGame = templateGame;
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

                streamGame = await _work.GameRepository.SingleOrDefaultAsync(i => i.ServiceType == stream.ServiceType && i.SourceId == stream.GameId);
            }

            if (!streamSubscriptions.Any())
                return;

            foreach (StreamSubscription streamSubscription in streamSubscriptions)
            {
                if (streamSubscription.DiscordGuild == null || streamSubscription.DiscordChannel == null)
                {
                    _logger.LogInformation("Removing orphaned Stream Subscription for {Username} on {ServiceType} - {SubscriptionId}", streamUser.Username, stream.ServiceType, streamSubscription.Id);

                    var rolesToMention = await _work.RoleToMentionRepository.FindAsync(i => i.StreamSubscription == streamSubscription);

                    foreach (var roleToMention in rolesToMention)
                        await _work.RoleToMentionRepository.RemoveAsync(roleToMention.Id);

                    await _work.SubscriptionRepository.RemoveAsync(streamSubscription.Id);

                    continue;
                }

                var discordChannel = streamSubscription.DiscordChannel;
                var discordGuild = streamSubscription.DiscordGuild;

                SocketGuild? guild = null;

                try
                {
                    guild = _client.GetGuild(streamSubscription.DiscordGuild.DiscordId);
                }
                catch (HttpException ex)
                {
                    if (ex.DiscordCode == DiscordErrorCode.InsufficientPermissions || ex.DiscordCode == DiscordErrorCode.MissingPermissions)
                    {
                        await _bus.Publish(new DiscordGuildDelete()
                        {
                            GuildId = streamSubscription.DiscordGuild.DiscordId,
                        });

                        continue;
                    }
                }

                if (guild == null)
                    continue;

                SocketTextChannel? channel = null;

                try
                {
                    channel = guild.GetTextChannel(streamSubscription.DiscordChannel.DiscordId);
                }
                catch (HttpException ex)
                {
                    if (ex.DiscordCode == DiscordErrorCode.InsufficientPermissions || ex.DiscordCode == DiscordErrorCode.MissingPermissions || ex.DiscordCode == DiscordErrorCode.UnknownChannel)
                    {
                        _logger.LogError(exception: ex, message: "Removing orphaned Stream Subscription for {Username} on {ServiceType} because channel could not be found in {GuildId} - {SubscriptionId}", streamUser.Username, stream.ServiceType, guild.Id, streamSubscription.Id);

                        var rolesToMention = await _work.RoleToMentionRepository.FindAsync(i => i.StreamSubscription == streamSubscription);

                        foreach (var roleToMention in rolesToMention)
                            await _work.RoleToMentionRepository.RemoveAsync(roleToMention.Id);

                        await _work.SubscriptionRepository.RemoveAsync(streamSubscription.Id);

                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }

                // If the channel can't be found (null)
                // but the subscription lives, assume guild/channel
                // offline and try again on next run
                if (channel == null)
                {
                    continue;
                }

                async Task RemoveSubscriptionAsync()
                {
                    _logger.LogInformation("Removing Stream Subscription for {Username} on {ServiceType} because missing permissions in {GuildId} {ChannelId} - {SubscriptionId}",
                        streamUser.Username,
                        stream.ServiceType,
                        guild?.Id ?? 0,
                        channel?.Id ?? 0,
                        streamSubscription.Id);

                    var rolesToMention = await _work.RoleToMentionRepository.FindAsync(i => i.StreamSubscription == streamSubscription);

                    foreach (var roleToMention in rolesToMention)
                        await _work.RoleToMentionRepository.RemoveAsync(roleToMention.Id);

                    await _work.SubscriptionRepository.RemoveAsync(streamSubscription.Id);
                }

                string notificationMessage = NotificationHelpers.GetNotificationMessage(guild: guild, stream: stream, subscription: streamSubscription, user: user, game: game);

                Embed embed = NotificationHelpers.GetStreamEmbed(stream: stream, user: user, game: game);

                var newStreamNotification = new StreamNotification
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

                TimeSpan lockTimeout = TimeSpan.FromSeconds(30);

                bool obtainedLock = false;

                string recordId = $"subscription:{stream.ServiceType}:{user.Id}:{guild.Id}";

                Guid lockGuid = Guid.NewGuid();

                do
                {
                    obtainedLock = await _cache.ObtainLockAsync(recordId: recordId, identifier: lockGuid, expiryTime: lockTimeout);
                }
                while (!obtainedLock);

                try
                {
                    var previousNotifications = (await _work.NotificationRepository.FindAsync(previousNotificationPredicate))
                        .Where(i => i.Success)
                        .OrderByDescending(i => i.Stream_StartTime)
                        .ToList();

                    var recentNotification = previousNotifications
                        .FirstOrDefault(i => Math.Abs((stream.StartTime - i.Stream_StartTime).TotalMinutes) < NotificationCooldownMinutes);

                    // Only perform message deletion and updating for beta servers
                    if (discordGuild.IsInBeta)
                    {
                        var staleNotifications = previousNotifications
                            .Where(i => (stream.StartTime - i.Stream_StartTime).TotalMinutes >= NotificationCooldownMinutes)
                            .ToList();

                        foreach (var stale in staleNotifications)
                        {
                            if (recentNotification != null && stale.Id == recentNotification.Id)
                                continue;

                            bool deleted = true;

                            if (stale.DiscordMessage_DiscordId.HasValue)
                            {
                                try
                                {
                                    await channel.DeleteMessageAsync(stale.DiscordMessage_DiscordId.Value);
                                }
                                catch (HttpException ex)
                                {
                                    if (ex.DiscordCode == DiscordErrorCode.UnknownMessage)
                                    {
                                        deleted = true;
                                    }
                                    else if (ex.DiscordCode == DiscordErrorCode.MissingPermissions || ex.DiscordCode == DiscordErrorCode.InsufficientPermissions)
                                    {
                                        deleted = false;

                                        _logger.LogWarning(
                                            ex,
                                            "Missing permissions to delete stale notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId}",
                                            stale.Id,
                                            stale.ServiceType,
                                            stale.User_Username,
                                            stale.DiscordGuild_DiscordId,
                                            stale.DiscordChannel_DiscordId);
                                    }
                                    else
                                    {
                                        deleted = false;

                                        _logger.LogWarning(
                                            ex,
                                            "Failed to delete stale notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId}",
                                            stale.Id,
                                            stale.ServiceType,
                                            stale.User_Username,
                                            stale.DiscordGuild_DiscordId,
                                            stale.DiscordChannel_DiscordId);
                                    }
                                }
                            }

                            if (deleted)
                            {
                                stale.DiscordMessage_DiscordId = null;
                                stale.Success = false;
                                stale.LogMessage = $"Deleted stale notification at {DateTime.UtcNow:o}";
                            }
                            else
                            {
                                stale.LogMessage = $"Unable to delete stale notification at {DateTime.UtcNow:o}";
                            }

                            await _work.NotificationRepository.UpdateAsync(stale);
                        }

                        // Beta server: Handle message updating
                        if (recentNotification != null)
                        {
                            if (recentNotification.DiscordMessage_DiscordId.HasValue)
                            {
                                IUserMessage? existingMessage = null;

                                try
                                {
                                    existingMessage = await channel.GetMessageAsync(recentNotification.DiscordMessage_DiscordId.Value) as IUserMessage;
                                }
                                catch (HttpException ex)
                                {
                                    if (ex.DiscordCode != DiscordErrorCode.UnknownMessage)
                                        _logger.LogWarning(ex, "Unable to fetch existing notification message for {NotificationId}", recentNotification.Id);
                                }

                                if (existingMessage != null && existingMessage.Author.Id == _client.CurrentUser.Id)
                                {
                                    var currentEmbed = existingMessage.Embeds.FirstOrDefault();

                                    bool shouldUpdate = HasNotificationChanged(recentNotification, newStreamNotification)
                                        || ShouldUpdateEmbed(currentEmbed, embed)
                                        || !string.Equals(existingMessage.Content ?? string.Empty, notificationMessage, StringComparison.Ordinal);

                                    if (shouldUpdate)
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
                                                await RemoveSubscriptionAsync();
                                                continue;
                                            }

                                            _logger.LogError(
                                                ex,
                                                "Error updating notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId}",
                                                recentNotification.Id,
                                                recentNotification.ServiceType,
                                                recentNotification.User_Username,
                                                recentNotification.DiscordGuild_DiscordId,
                                                recentNotification.DiscordChannel_DiscordId);

                                            throw;
                                        }

                                        _logger.LogInformation(
                                            "Updated notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId}",
                                            recentNotification.Id,
                                            recentNotification.ServiceType,
                                            recentNotification.User_Username,
                                            recentNotification.DiscordGuild_DiscordId,
                                            recentNotification.DiscordChannel_DiscordId);
                                    }

                                    CopyNotificationValues(recentNotification, newStreamNotification);

                                    recentNotification.DiscordMessage_DiscordId = existingMessage.Id;

                                    recentNotification.LogMessage = $"Updated at {DateTime.UtcNow:o}";

                                    recentNotification.Success = true;

                                    await _work.NotificationRepository.UpdateAsync(recentNotification);

                                    continue;
                                }

                                recentNotification.DiscordMessage_DiscordId = null;

                                CopyNotificationValues(recentNotification, newStreamNotification);

                                recentNotification.Success = true;

                                recentNotification.LogMessage = $"Existing notification message missing or inaccessible at {DateTime.UtcNow:o}. Cooldown active.";

                                await _work.NotificationRepository.UpdateAsync(recentNotification);

                                _logger.LogInformation(
                                    "Skipping new notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId} due to active cooldown window.",
                                    recentNotification.Id,
                                    recentNotification.ServiceType,
                                    recentNotification.User_Username,
                                    recentNotification.DiscordGuild_DiscordId,
                                    recentNotification.DiscordChannel_DiscordId);

                                continue;
                            }

                            CopyNotificationValues(recentNotification, newStreamNotification);

                            recentNotification.Success = true;

                            recentNotification.LogMessage = $"Cooldown active at {DateTime.UtcNow:o}. Suppressing duplicate notification.";

                            await _work.NotificationRepository.UpdateAsync(recentNotification);

                            _logger.LogInformation(
                                "Skipping new notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId} due to active cooldown window.",
                                recentNotification.Id,
                                recentNotification.ServiceType,
                                recentNotification.User_Username,
                                recentNotification.DiscordGuild_DiscordId,
                                recentNotification.DiscordChannel_DiscordId);

                            continue;
                        }
                    }
                    else
                    {
                        // Non-beta server: Use standard 60-minute cooldown logic
                        if (recentNotification != null)
                        {
                            CopyNotificationValues(recentNotification, newStreamNotification);

                            recentNotification.Success = true;

                            recentNotification.LogMessage = $"Cooldown active at {DateTime.UtcNow:o}. Suppressing duplicate notification.";

                            await _work.NotificationRepository.UpdateAsync(recentNotification);

                            _logger.LogInformation(
                                "Skipping new notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId} due to active cooldown window.",
                                recentNotification.Id,
                                recentNotification.ServiceType,
                                recentNotification.User_Username,
                                recentNotification.DiscordGuild_DiscordId,
                                recentNotification.DiscordChannel_DiscordId);

                            continue;
                        }
                    }

                    await _work.NotificationRepository.AddOrUpdateAsync(newStreamNotification, notificationPredicate);

                    var streamNotification = await _work.NotificationRepository.SingleOrDefaultAsync(notificationPredicate);

                    if (streamNotification.Success == true)
                        continue;

                    var notificationDelay = (DateTime.UtcNow - streamNotification.Stream_StartTime).TotalMilliseconds;

                    if (streamNotification.Stream_StartTime < streamSubscription.TimeStamp)
                        notificationDelay = (DateTime.UtcNow - streamSubscription.TimeStamp).TotalMilliseconds;

                    try
                    {
                        CancellationTokenSource cancellationToken = new();

                        cancellationToken.CancelAfter((int)lockTimeout.TotalMilliseconds);

                        var messageRequestOptions = new RequestOptions()
                        {
                            RetryMode = RetryMode.AlwaysFail,
                            Timeout = (int)lockTimeout.TotalMilliseconds,
                            CancelToken = cancellationToken.Token
                        };

                        var discordMessage = await channel.SendMessageAsync(text: notificationMessage, embed: embed, options: messageRequestOptions);

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
                    catch (Exception ex)
                    {
                        streamNotification.LogMessage = $"Error sending notification at {DateTime.UtcNow:o}: {ex.Message}";

                        await _work.NotificationRepository.UpdateAsync(streamNotification);

                        if (ex is HttpException httpException)
                        {
                            if (httpException.DiscordCode == DiscordErrorCode.MissingPermissions || httpException.DiscordCode == DiscordErrorCode.InsufficientPermissions)
                            {
                                await RemoveSubscriptionAsync();
                            }
                            else
                            {
                                _logger.LogError(
                                    exception: ex,
                                    message: "Error sending notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId} {@RoleIds}, {Message} {IsFromRole}, {MillisecondsToPost}",
                                    streamNotification.Id,
                                    streamNotification.ServiceType,
                                    streamNotification.User_Username,
                                    streamNotification.DiscordGuild_DiscordId.ToString(),
                                    streamNotification.DiscordChannel_DiscordId.ToString(),
                                    streamSubscription.RolesToMention.Select(i => i.DiscordRoleId.ToString()).Distinct().ToList(),
                                    streamNotification.Message,
                                    false,
                                    notificationDelay
                                );
                            }
                        }
                        else
                        {
                            _logger.LogError(
                                exception: ex,
                                message: "Error sending notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId} {@RoleIds}, {Message} {IsFromRole}, {MillisecondsToPost}",
                                streamNotification.Id,
                                streamNotification.ServiceType,
                                streamNotification.User_Username,
                                streamNotification.DiscordGuild_DiscordId.ToString(),
                                streamNotification.DiscordChannel_DiscordId.ToString(),
                                streamSubscription.RolesToMention.Select(i => i.DiscordRoleId.ToString()).Distinct().ToList(),
                                streamNotification.Message,
                                false,
                                notificationDelay
                            );
                        }
                    }
                }
                finally
                {
                    if (obtainedLock)
                        await _cache.ReleaseLockAsync(recordId, identifier: lockGuid);
                }
            }
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