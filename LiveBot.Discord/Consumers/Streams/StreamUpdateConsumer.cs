using Discord.WebSocket;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Discord.Helpers;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers.Streams
{
    public class StreamUpdateConsumer : IConsumer<IStreamUpdate>
    {
        private readonly DiscordShardedClient _client;
        private readonly IUnitOfWork _work;
        private readonly IBusControl _bus;
        private readonly List<ILiveBotMonitor> _monitors;

        public StreamUpdateConsumer(DiscordShardedClient client, IUnitOfWorkFactory factory, IBusControl bus, List<ILiveBotMonitor> monitors)
        {
            _client = client;
            _work = factory.Create();
            _bus = bus;
            _monitors = monitors;
        }

        public async Task Consume(ConsumeContext<IStreamUpdate> context)
        {
            ILiveBotStream messageStream = context.Message.Stream;
            ILiveBotMonitor monitor = _monitors.Where(i => i.ServiceType == messageStream.ServiceType).FirstOrDefault();

            if (monitor == null)
                return;

            ILiveBotStream stream = await monitor.GetStream(messageStream.UserId);
            if (stream == null)
                return;

            ILiveBotUser user = stream.User ?? await monitor.GetUser(stream.UserId);
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

            List<StreamSubscription> unsentSubscriptions = new List<StreamSubscription>();

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

                Expression<Func<StreamNotification, bool>> previousNotificationPredicate = (i =>
                    i.User_SourceID == streamUser.SourceID &&
                    i.DiscordGuild_DiscordId == discordGuild.DiscordId &&
                    i.DiscordChannel_DiscordId == discordChannel.DiscordId
                );

                var previousStreamNotifications = await _work.NotificationRepository.FindInOrderAsync(previousNotificationPredicate, i => i.Id);

                var previousNotifications = previousStreamNotifications.Where(i =>
                    i.DiscordGuild_DiscordId == discordGuild.DiscordId &&
                    i.Stream_SourceID == stream.Id &&
                    i.Stream_StartTime == stream.StartTime &&
                    i.Success == true
                );

                if (previousNotifications.Count() > 0)
                {
                    var previousStreamNotification = previousStreamNotifications.LastOrDefault();
                    bool createNewNotification = false;
                    if (previousNotifications.Where(i => i.Game_SourceID == streamGame.SourceId).Count() == 0)
                        createNewNotification = true;
                    if (previousNotifications.Where(i => i.Stream_Title == stream.Title).Count() == 0)
                        createNewNotification = true;

                    if (createNewNotification)
                    {
                        string notificationMessage = NotificationHelpers.GetNotificationMessage(stream: stream, subscription: streamSubscription, user: user, game: game);

                        StreamNotification newStreamNotification = new StreamNotification();
                        newStreamNotification.ServiceType = previousStreamNotification.ServiceType;
                        newStreamNotification.Success = true;
                        newStreamNotification.Message = notificationMessage;

                        newStreamNotification.User_SourceID = streamUser.SourceID;
                        newStreamNotification.User_Username = streamUser.Username;
                        newStreamNotification.User_DisplayName = streamUser.DisplayName;
                        newStreamNotification.User_AvatarURL = streamUser.AvatarURL;
                        newStreamNotification.User_ProfileURL = streamUser.ProfileURL;

                        newStreamNotification.Stream_SourceID = stream.Id;
                        newStreamNotification.Stream_Title = stream.Title;
                        newStreamNotification.Stream_StartTime = stream.StartTime;
                        newStreamNotification.Stream_ThumbnailURL = stream.ThumbnailURL;
                        newStreamNotification.Stream_StreamURL = stream.StreamURL;

                        newStreamNotification.Game_SourceID = streamGame?.SourceId;
                        newStreamNotification.Game_Name = streamGame?.Name;
                        newStreamNotification.Game_ThumbnailURL = streamGame?.ThumbnailURL;

                        newStreamNotification.DiscordGuild_DiscordId = previousStreamNotification.DiscordGuild_DiscordId;
                        newStreamNotification.DiscordGuild_Name = previousStreamNotification.DiscordGuild_Name;

                        newStreamNotification.DiscordChannel_DiscordId = previousStreamNotification.DiscordChannel_DiscordId;
                        newStreamNotification.DiscordChannel_Name = previousStreamNotification.DiscordChannel_Name;

                        newStreamNotification.DiscordRole_DiscordId = previousStreamNotification.DiscordRole_DiscordId;
                        newStreamNotification.DiscordRole_Name = previousStreamNotification.DiscordRole_Name;

                        newStreamNotification.DiscordMessage_DiscordId = previousStreamNotification.DiscordMessage_DiscordId;

                        Expression<Func<StreamNotification, bool>> notificationPredicate = (i =>
                            i.User_SourceID == newStreamNotification.User_SourceID &&
                            i.Stream_SourceID == newStreamNotification.Stream_SourceID &&
                            i.Stream_StartTime == newStreamNotification.Stream_StartTime &&
                            i.DiscordGuild_DiscordId == newStreamNotification.DiscordGuild_DiscordId &&
                            i.DiscordChannel_DiscordId == newStreamNotification.DiscordChannel_DiscordId &&
                            i.Game_SourceID == newStreamNotification.Game_SourceID &&
                            i.Stream_Title == newStreamNotification.Stream_Title
                        );

                        await _work.NotificationRepository.AddOrUpdateAsync(newStreamNotification, notificationPredicate);
                    }

                    continue;
                }

                unsentSubscriptions.Add(streamSubscription);
            }

            if (unsentSubscriptions.Count() > 0)
            {
                await _bus.Publish<IStreamOnline>(new { Stream = stream });
            }
        }
    }
}