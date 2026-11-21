using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Watcher.Twitch.Contracts;
using LiveBot.Watcher.Twitch.Models;
using MassTransit;

namespace LiveBot.Watcher.Twitch.Consumers
{
    /// <summary>
    /// Consumer that handles startup catch-up to detect streams that went offline while the watcher was down
    /// </summary>
    public class TwitchStartupCatchupConsumer : IConsumer<TwitchStartupCatchup>
    {
        private readonly ILogger<TwitchStartupCatchupConsumer> _logger;
        private readonly TwitchMonitor _monitor;
        private readonly IUnitOfWorkFactory _workFactory;

        public TwitchStartupCatchupConsumer(
            ILogger<TwitchStartupCatchupConsumer> logger,
            TwitchMonitor monitor,
            IUnitOfWorkFactory workFactory)
        {
            _logger = logger;
            _monitor = monitor;
            _workFactory = workFactory;
        }

        public async Task Consume(ConsumeContext<TwitchStartupCatchup> context)
        {
            try
            {
                _logger.LogInformation("Processing startup catch-up for {UserCount} users on {ServiceType}",
                    context.Message.StreamUsers.Count(), context.Message.ServiceType);

                var work = _workFactory.Create();

                var users = context.Message.StreamUsers.ToList();
                var (liveStreams, failedLookups) = await _monitor.GetLiveStreamsByUserIds(users.Select(u => u.Id).ToList());

                if (failedLookups.Count > 0)
                {
                    _logger.LogWarning("Skipping catch-up for {FailedCount} users on {ServiceType} because their stream lookup failed",
                        failedLookups.Count, context.Message.ServiceType);
                }

                foreach (var user in users)
                {
                    // An unanswered lookup says nothing about the stream, so it must not read as offline.
                    if (failedLookups.Contains(user.Id))
                        continue;

                    try
                    {
                        await ProcessUserCatchup(user, liveStreams.ContainsKey(user.Id), work);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing catch-up for user {UserId} ({Username})",
                            user.Id, user.Username);
                    }
                }

                _logger.LogInformation("Completed startup catch-up for {ServiceType}", context.Message.ServiceType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during startup catch-up processing for {ServiceType}",
                    context.Message.ServiceType);
            }
        }

        private async Task ProcessUserCatchup(Core.Repository.Interfaces.Monitor.ILiveBotUser user, bool isCurrentlyLive, IUnitOfWork work)
        {
            // The regular monitor picks up streams that are live now, so only the offline case matters here.
            if (isCurrentlyLive)
                return;

            var dbUser = await work.UserRepository.SingleOrDefaultAsync(u =>
                u.ServiceType == _monitor.ServiceType && u.SourceID == user.Id);

            if (dbUser == null)
            {
                _logger.LogDebug("User {UserId} not found in database during catch-up", user.Id);
                return;
            }

            var lastNotification = await GetUnmarkedLatestStreamNotification(dbUser, work);

            _logger.LogDebug("Catch-up for {Username}: WasLive={WasLive}",
                user.Username, lastNotification != null);

            if (lastNotification == null)
                return;

            _logger.LogInformation("Detected offline transition for {Username} during startup catch-up", user.Username);

            var offlineStream = await CreateStreamFromLastNotification(lastNotification, user);
            if (offlineStream != null)
            {
                await _monitor.PublishStreamOffline(offlineStream);
                _logger.LogInformation("Published offline event for {Username} during startup catch-up", user.Username);
            }
            else
            {
                _logger.LogWarning("Failed to create stream object for offline event for {Username}", user.Username);
            }
        }

        /// <summary>
        /// Gets the newest notification of the user's most recent stream, or null when every message for
        /// that stream that still has a subscription has already been marked offline
        /// </summary>
        private async Task<StreamNotification?> GetUnmarkedLatestStreamNotification(StreamUser dbUser, IUnitOfWork work)
        {
            // Ordered by Stream_StartTime because a row reused for a restarted stream keeps its old TimeStamp.
            var latest = await work.NotificationRepository.FirstOrderedAsync(n =>
                n.ServiceType == dbUser.ServiceType &&
                n.User_SourceID == dbUser.SourceID &&
                n.Success == true &&
                n.DiscordMessage_DiscordId != null,
                n => n.Stream_StartTime);

            if (latest == null)
                return null;

            // Filtering on OfflineAt in the first query could pick an older stream's leftover row instead.
            var unmarked = await work.NotificationRepository.FindAsync(n =>
                n.ServiceType == dbUser.ServiceType &&
                n.User_SourceID == dbUser.SourceID &&
                n.Stream_SourceID == latest.Stream_SourceID &&
                n.Success == true &&
                n.DiscordMessage_DiscordId != null &&
                n.OfflineAt == null);

            // The offline consumer never marks a row whose subscription is gone, so such a row would
            // otherwise trigger an offline event on every restart.
            foreach (var notification in unmarked)
            {
                var hasSubscription = await work.SubscriptionRepository.AnyAsync(s =>
                    s.User.Id == dbUser.Id &&
                    s.DiscordGuild.DiscordId == notification.DiscordGuild_DiscordId &&
                    s.DiscordChannel.DiscordId == notification.DiscordChannel_DiscordId);

                if (hasSubscription)
                    return latest;
            }

            return null;
        }

        /// <summary>
        /// Creates a stream object from the last known notification for offline event
        /// </summary>
        private async Task<Core.Repository.Interfaces.Monitor.ILiveBotStream?> CreateStreamFromLastNotification(
            StreamNotification lastNotification,
            Core.Repository.Interfaces.Monitor.ILiveBotUser user)
        {
            try
            {
                // Create a minimal stream object for the offline event
                // We don't have all the original stream data, but we have enough for an offline event
                var game = await _monitor.GetGame(lastNotification.Game_SourceID ?? "");

                // Create a TwitchStream for the offline event using the parameterless constructor
                // and manually setting the required properties
                var offlineStream = new TwitchStream(_monitor.BaseURL, _monitor.ServiceType)
                {
                    Id = lastNotification.Stream_SourceID ?? "",
                    UserId = user.Id,
                    User = user,
                    GameId = game.Id,
                    Game = game,
                    Title = lastNotification.Stream_Title ?? "Unknown Title",
                    StartTime = lastNotification.Stream_StartTime != DateTime.MinValue
                        ? lastNotification.Stream_StartTime
                        : lastNotification.TimeStamp,
                    ThumbnailURL = lastNotification.Stream_ThumbnailURL ?? "",
                    StreamURL = user.ProfileURL ?? ""
                };

                return offlineStream;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error creating stream object from last notification for user {UserId}", user.Id);
                return null;
            }
        }
    }
}