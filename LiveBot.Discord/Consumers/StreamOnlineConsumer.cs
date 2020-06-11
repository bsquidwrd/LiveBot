using Discord;
using Discord.WebSocket;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Discord.Helpers;
using MassTransit;
using Serilog;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers
{
    public class StreamOnlineConsumer : IConsumer<IStreamOnline>
    {
        private readonly DiscordShardedClient _client;
        private readonly IUnitOfWork _work;

        public StreamOnlineConsumer(DiscordShardedClient client, IUnitOfWorkFactory factory)
        {
            _client = client;
            _work = factory.Create();
        }

        public async Task Consume(ConsumeContext<IStreamOnline> context)
        {
            ILiveBotStream stream = context.Message.Stream;
            var streamUser = await _work.UserRepository.SingleOrDefaultAsync(i => i.ServiceType == stream.ServiceType && i.SourceID == stream.User.Id);
            var streamSubscriptions = await _work.SubscriptionRepository.FindAsync(i => i.User == streamUser);

            foreach (StreamSubscription streamSubscription in streamSubscriptions)
            {
                Expression<Func<StreamNotification, bool>> notificationPredicate = (i =>
                    i.User_SourceID == streamSubscription.User.SourceID &&
                    i.Stream_SourceID == stream.Id &&
                    i.Stream_StartTime == stream.StartTime &&
                    i.DiscordGuild_DiscordId == streamSubscription.DiscordChannel.DiscordGuild.DiscordId &&
                    i.DiscordChannel_DiscordId == streamSubscription.DiscordChannel.DiscordId
                );

                Expression<Func<StreamNotification, bool>> previousNotificationPredicate = (i =>
                    i.User_SourceID == streamSubscription.User.SourceID &&
                    i.DiscordGuild_DiscordId == streamSubscription.DiscordChannel.DiscordGuild.DiscordId &&
                    i.DiscordChannel_DiscordId == streamSubscription.DiscordChannel.DiscordId
                );

                SocketTextChannel channel = (SocketTextChannel)_client.GetChannel(streamSubscription.DiscordChannel.DiscordId);
                string notificationMessage = NotificationHelpers.GetNotificationMessage(stream, streamSubscription);
                Embed embed = NotificationHelpers.GetStreamEmbed(stream);

                StreamNotification streamNotification = new StreamNotification
                {
                    ServiceType = stream.ServiceType,
                    Success = false,
                    Message = notificationMessage,

                    User_SourceID = streamSubscription.User.SourceID,
                    User_Username = streamSubscription.User.Username,
                    User_DisplayName = streamSubscription.User.DisplayName,
                    User_AvatarURL = streamSubscription.User.AvatarURL,
                    User_ProfileURL = streamSubscription.User.ProfileURL,

                    Stream_SourceID = stream.Id,
                    Stream_Title = stream.Title,
                    Stream_StartTime = stream.StartTime,
                    Stream_ThumbnailURL = stream.ThumbnailURL,
                    Stream_StreamURL = stream.StreamURL,

                    Game_SourceID = stream.Game.Id,
                    Game_Name = stream.Game.Name,
                    Game_ThumbnailURL = stream.Game.ThumbnailURL,

                    DiscordGuild_DiscordId = streamSubscription.DiscordChannel.DiscordGuild.DiscordId,
                    DiscordGuild_Name = streamSubscription.DiscordChannel.DiscordGuild.Name,

                    DiscordChannel_DiscordId = streamSubscription.DiscordChannel.DiscordId,
                    DiscordChannel_Name = streamSubscription.DiscordChannel.Name,

                    DiscordRole_DiscordId = streamSubscription.DiscordRole == null ? 0 : streamSubscription.DiscordRole.DiscordId,
                    DiscordRole_Name = streamSubscription.DiscordRole?.Name
                };

                var previousNotifications = await _work.NotificationRepository.FindAsync(previousNotificationPredicate);
                previousNotifications = previousNotifications.Where(i =>
                    i.Stream_StartTime.Subtract(i.Stream_StartTime).TotalMinutes <= 60 && // If within an hour of their last start time
                    i.Success == true
                );

                if (previousNotifications.Count() > 0)
                    streamNotification.Success = true;

                await _work.NotificationRepository.AddOrUpdateAsync(streamNotification, notificationPredicate);
                streamNotification = await _work.NotificationRepository.SingleOrDefaultAsync(notificationPredicate);

                if (streamNotification.Success == true)
                    return;

                try
                {
                    var discordMessage = await channel.SendMessageAsync(text: notificationMessage, embed: embed);
                    streamNotification.Success = true;
                    streamNotification.DiscordMessage_DiscordId = discordMessage.Id;
                    await _work.NotificationRepository.UpdateAsync(streamNotification);
                }
                catch (Exception e)
                {
                    Log.Error($"Error sending notification for {streamNotification.Id} {streamNotification.ServiceType} {streamNotification.User_Username} {streamNotification.DiscordGuild_DiscordId} {streamNotification.DiscordChannel_DiscordId} {streamNotification.DiscordRole_DiscordId} {streamNotification.Message}\n{e}");
                }
            }
        }
    }
}