using Discord;
using Discord.Net;
using Discord.WebSocket;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Discord.SlashCommands.Contracts.Discord;
using LiveBot.Discord.SlashCommands.Helpers;
using MassTransit;
using System.Linq.Expressions;

namespace LiveBot.Discord.SlashCommands.Consumers.Streams
{
    public class StreamOnlineConsumer : IConsumer<IStreamOnline>
    {
        private readonly ILogger<StreamOnlineConsumer> _logger;
        private readonly DiscordShardedClient _client;
        private readonly IUnitOfWork _work;
        private readonly IBus _bus;

        public StreamOnlineConsumer(ILogger<StreamOnlineConsumer> logger, DiscordShardedClient client, IUnitOfWorkFactory factory, IBus bus)
        {
            _logger = logger;
            _client = client;
            _work = factory.Create();
            _bus = bus;
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

            if (streamSubscriptions.Count() == 0)
                return;

            foreach (StreamSubscription streamSubscription in streamSubscriptions)
            {
                if (streamSubscription.DiscordGuild == null || streamSubscription.DiscordChannel == null)
                {
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
                    )
                    {
                        var rolesToMention = await _work.RoleToMentionRepository.FindAsync(i => i.StreamSubscription == streamSubscription);
                        foreach (var roleToMention in rolesToMention)
                            await _work.RoleToMentionRepository.RemoveAsync(roleToMention.Id);
                        await _work.SubscriptionRepository.RemoveAsync(streamSubscription.Id);
                        continue;
                    }
                    else if (ex.DiscordCode == DiscordErrorCode.UnknownChannel)
                    {
                        // It's possible the guild is unavailable
                        // continue on and let GuildAvailable events
                        // handle the cleanups
                        continue;
                    }
                    else
                    {
                        throw;
                    }
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

                // If the channel can't be found (null) and the shard
                // is online, check if the Guild is online
                if (channel == null)
                {
                    // If the Guild is online, but the channel isn't found
                    // remove the subscription
                    if (guild != null)
                    {
                        var rolesToMention = await _work.RoleToMentionRepository.FindAsync(i => i.StreamSubscription == streamSubscription);
                        foreach (var roleToMention in rolesToMention)
                            await _work.RoleToMentionRepository.RemoveAsync(roleToMention.Id);
                        await _work.SubscriptionRepository.RemoveAsync(streamSubscription.Id);
                    }
                    return;
                }

                await _work.NotificationRepository.AddOrUpdateAsync(newStreamNotification, notificationPredicate);
                StreamNotification streamNotification = await _work.NotificationRepository.SingleOrDefaultAsync(notificationPredicate);

                // If the current notification was marked as a success, end processing
                if (streamNotification.Success == true)
                    continue;

                try
                {
                    var discordMessage = await channel.SendMessageAsync(text: notificationMessage, embed: embed);
                    streamNotification.DiscordMessage_DiscordId = discordMessage.Id;
                    streamNotification.Success = true;
                    await _work.NotificationRepository.UpdateAsync(streamNotification);

                    _logger.LogInformation(
                        message: "Sent notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId} {@RoleIds}, {Message} {IsFromRole}, {TimeToPost}",
                        streamNotification.Id,
                        streamNotification.ServiceType,
                        streamNotification.User_Username,
                        streamNotification.DiscordGuild_DiscordId.ToString(),
                        streamNotification.DiscordChannel_DiscordId.ToString(),
                        streamNotification.DiscordRole_Name.Split(","),
                        streamNotification.Message,
                        false,
                        DateTime.UtcNow - streamNotification.Stream_StartTime
                    );
                }
                catch (Exception ex)
                {
                    if (ex is HttpException discordError)
                    {
                        // You lack permissions to perform that action
                        if (
                            discordError.DiscordCode == DiscordErrorCode.InsufficientPermissions
                            || discordError.DiscordCode == DiscordErrorCode.MissingPermissions
                        )
                        {
                            // I'm tired of seeing errors for Missing Permissions
                            continue;
                        }
                    }
                    else
                    {
                        _logger.LogError(
                            exception: ex,
                            message: "Error sending notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId} {@RoleIds}, {Message} {IsFromRole}, {TimeToPost}",
                            streamNotification.Id,
                            streamNotification.ServiceType,
                            streamNotification.User_Username,
                            streamNotification.DiscordGuild_DiscordId.ToString(),
                            streamNotification.DiscordChannel_DiscordId.ToString(),
                            streamSubscription.RolesToMention.Select(i => i.DiscordRoleId.ToString()).Distinct().ToList(),
                            streamNotification.Message,
                            false,
                            DateTime.UtcNow - streamNotification.Stream_StartTime
                        );
                    }
                }
            }
        }
    }
}
