using Discord;
using Discord.Net;
using Discord.WebSocket;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Discord.Helpers;
using MassTransit;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers.Streams
{
    public class StreamOnlineConsumer : IConsumer<IStreamOnline>
    {
        private readonly DiscordShardedClient _client;
        private readonly IUnitOfWork _work;
        private readonly IBusControl _bus;
        private readonly IEnumerable<ILiveBotMonitor> _monitors;

        public StreamOnlineConsumer(DiscordShardedClient client, IUnitOfWorkFactory factory, IBusControl bus, IEnumerable<ILiveBotMonitor> monitors)
        {
            _client = client;
            _work = factory.Create();
            _bus = bus;
            _monitors = monitors;
        }

        public async Task Consume(ConsumeContext<IStreamOnline> context)
        {
            ILiveBotStream stream = context.Message.Stream;
            ILiveBotMonitor monitor = _monitors.Where(i => i.ServiceType == stream.ServiceType).FirstOrDefault();

            if (monitor == null)
                return;

            ILiveBotUser user = stream.User ?? await monitor.GetUserById(stream.UserId);
            ILiveBotGame game = stream.Game ?? await monitor.GetGame(stream.GameId);

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
                StreamGame newStreamGame = new StreamGame
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
                    await _work.SubscriptionRepository.RemoveAsync(streamSubscription.Id);
                    continue;
                }

                var discordChannel = streamSubscription.DiscordChannel;
                var discordRole = streamSubscription.DiscordRole;
                var discordGuild = streamSubscription.DiscordGuild;

                var guild = _client.GetGuild(streamSubscription.DiscordGuild.DiscordId);
                SocketTextChannel channel = (SocketTextChannel)_client.GetChannel(streamSubscription.DiscordChannel.DiscordId);

                if (guild == null)
                    return;

                string notificationMessage = NotificationHelpers.GetNotificationMessage(stream: stream, subscription: streamSubscription, user: user, game: game);
                Embed embed = NotificationHelpers.GetStreamEmbed(stream: stream, user: user, game: game);

                StreamNotification newStreamNotification = new StreamNotification
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

                    DiscordRole_DiscordId = discordRole == null ? 0 : discordRole.DiscordId,
                    DiscordRole_Name = discordRole?.Name
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
                if (previousNotifications.Count() > 0)
                    newStreamNotification.Success = true;

                // If the channel can't be found (null) and the shard
                // is online, check if the Guild is online
                if (channel == null && _client.GetShardFor(guild).LoginState == LoginState.LoggedIn)
                {
                    // If the Guild is online, but the channel isn't found
                    // remove the subscription
                    if (guild.IsConnected)
                    {
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
                }
                catch (Exception e)
                {
                    if (e is HttpException discordError)
                    {
                        // You lack permissions to perform that action
                        if (discordError.DiscordCode == 50013)
                        {
                            // I'm tired of seeing errors for Missing Permissions
                            continue;
                        }
                    }
                    else
                    {
                        Log.Error($"Error sending notification for {streamNotification.Id} {streamNotification.ServiceType} {streamNotification.User_Username} {streamNotification.DiscordGuild_DiscordId} {streamNotification.DiscordChannel_DiscordId} {streamNotification.DiscordRole_DiscordId} {streamNotification.Message}\n{e}");
                    }
                }
            }
        }
    }
}