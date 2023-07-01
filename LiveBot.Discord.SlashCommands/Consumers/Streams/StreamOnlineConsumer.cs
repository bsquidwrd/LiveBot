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
using System.Linq.Expressions;

namespace LiveBot.Discord.SlashCommands.Consumers.Streams
{
    public class StreamOnlineConsumer : IConsumer<IStreamOnline>
    {
        private readonly ILogger<StreamOnlineConsumer> _logger;
        private readonly DiscordShardedClient _client;
        private readonly IUnitOfWork _work;
        private readonly IBus _bus;
        private readonly ConnectionMultiplexer _cache;

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
                    if (
                        ex.DiscordCode == DiscordErrorCode.InsufficientPermissions
                        || ex.DiscordCode == DiscordErrorCode.MissingPermissions
                    )
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
                    if (
                        ex.DiscordCode == DiscordErrorCode.InsufficientPermissions
                        || ex.DiscordCode == DiscordErrorCode.MissingPermissions
                        || ex.DiscordCode == DiscordErrorCode.UnknownChannel
                    )
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

                var previousNotifications = await _work.NotificationRepository.FindAsync(previousNotificationPredicate);
                previousNotifications = previousNotifications.Where(i =>
                    stream.StartTime.Subtract(i.Stream_StartTime).TotalMinutes <= 60 // If within an hour of their last start time
                    && i.Success == true // Only pull Successful notifications
                );

                // If there is already 1 or more notifications that were successful in the past hour
                // mark this current one as a success
                if (previousNotifications.Any())
                    newStreamNotification.Success = true;

                await _work.NotificationRepository.AddOrUpdateAsync(newStreamNotification, notificationPredicate);
                StreamNotification streamNotification = await _work.NotificationRepository.SingleOrDefaultAsync(notificationPredicate);

                // If the current notification was marked as a success, end processing
                if (streamNotification.Success == true)
                    continue;

                // Lock for user and guild
                TimeSpan lockTimeout = TimeSpan.FromMinutes(1);
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
                    previousNotifications = await _work.NotificationRepository.FindAsync(previousNotificationPredicate);
                    previousNotifications = previousNotifications.Where(i =>
                        stream.StartTime.Subtract(i.Stream_StartTime).TotalMinutes <= 60 // If within an hour of their last start time
                        && i.Success == true // Only pull Successful notifications
                    );

                    // If there is already 1 or more notifications that were successful in the past hour
                    // mark this current one as a success
                    if (previousNotifications.Any())
                    {
                        streamNotification.Success = true;
                        await _work.NotificationRepository.AddOrUpdateAsync(streamNotification, notificationPredicate);
                    }

                    if (!streamNotification.Success)
                    {
                        var notificationDelay = (DateTime.UtcNow - streamNotification.Stream_StartTime).TotalMilliseconds;
                        // If the subscription was created after the stream was live
                        // don't let it count against us in our stats!
                        if (streamNotification.Stream_StartTime < streamSubscription.TimeStamp)
                            notificationDelay = (DateTime.UtcNow - streamSubscription.TimeStamp).TotalMilliseconds;

                        try
                        {
                            // Setup a cancellation token
                            CancellationTokenSource cancellationToken = new();
                            System.Timers.Timer cancellationTimer = new()
                            {
                                AutoReset = false,
                                Enabled = true,
                                Interval = lockTimeout.Milliseconds
                            };
                            cancellationTimer.Elapsed += (sender, e) =>
                            {
                                cancellationToken.Cancel();
                            };

                            var messageRequestOptions = new RequestOptions()
                            {
                                RetryMode = RetryMode.AlwaysFail,
                                Timeout = lockTimeout.Milliseconds,
                                CancelToken = cancellationToken.Token
                            };
                            var discordMessage = await channel.SendMessageAsync(text: notificationMessage, embed: embed, options: messageRequestOptions);
                            streamNotification.DiscordMessage_DiscordId = discordMessage.Id;
                            streamNotification.Success = true;
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
                            if (ex is HttpException httpException)
                            {
                                if (
                                    httpException.DiscordCode == DiscordErrorCode.MissingPermissions
                                    || httpException.DiscordCode == DiscordErrorCode.InsufficientPermissions
                                )
                                {
                                    _logger.LogInformation("Removing Stream Subscription for {Username} on {ServiceType} because missing permissions in {GuildId} {ChannelId} - {SubscriptionId}", streamUser.Username, stream.ServiceType, guild.Id, channel.Id, streamSubscription.Id);
                                    var rolesToMention = await _work.RoleToMentionRepository.FindAsync(i => i.StreamSubscription == streamSubscription);
                                    foreach (var roleToMention in rolesToMention)
                                        await _work.RoleToMentionRepository.RemoveAsync(roleToMention.Id);
                                    await _work.SubscriptionRepository.RemoveAsync(streamSubscription.Id);
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
                }
                finally
                {
                    if (obtainedLock)
                        await _cache.ReleaseLockAsync(recordId, identifier: lockGuid);
                }
            }
        }
    }
}
