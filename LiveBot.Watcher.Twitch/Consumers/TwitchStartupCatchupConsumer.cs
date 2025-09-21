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

                foreach (var user in context.Message.StreamUsers)
                {
                    try
                    {
                        await ProcessUserCatchup(user, work);
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

        private async Task ProcessUserCatchup(Core.Repository.Interfaces.Monitor.ILiveBotUser user, IUnitOfWork work)
        {
            // Get the user's current stream state from the database
            var dbUser = await work.UserRepository.SingleOrDefaultAsync(u =>
                u.ServiceType == _monitor.ServiceType && u.SourceID == user.Id);

            if (dbUser == null)
            {
                _logger.LogDebug("User {UserId} not found in database during catch-up", user.Id);
                return;
            }

            // Check if the user was marked as live in the database
            var wasLive = await WasUserLiveBeforeShutdown(dbUser, work);
            
            // Check the current live state from Twitch API
            var currentStream = await _monitor.GetStream_Force(user);
            var isCurrentlyLive = currentStream != null;

            _logger.LogDebug("Catch-up for {Username}: WasLive={WasLive}, CurrentlyLive={CurrentlyLive}",
                user.Username, wasLive, isCurrentlyLive);

            // If the user was live but is now offline, fire the offline event
            if (wasLive && !isCurrentlyLive)
            {
                _logger.LogInformation("Detected offline transition for {Username} during startup catch-up", user.Username);
                
                // Create a synthetic stream object for the offline event
                // We need to get the last known stream info from the database
                var lastNotification = await GetLastStreamNotification(dbUser, work);
                if (lastNotification != null)
                {
                    var offlineStream = await CreateStreamFromLastNotification(lastNotification, user);
                    if (offlineStream != null)
                    {
                        await _monitor.PublishStreamOffline(offlineStream);
                        _logger.LogInformation("Published offline event for {Username} during startup catch-up", user.Username);
                    }
                }
            }
            // Note: We don't need to handle the online case here because the regular monitor will pick that up
        }

        /// <summary>
        /// Determines if a user was live before the watcher shutdown by checking recent notifications
        /// </summary>
        private async Task<bool> WasUserLiveBeforeShutdown(StreamUser dbUser, IUnitOfWork work)
        {
            // Look for recent stream notifications (within the last 24 hours)
            var cutoffTime = DateTime.UtcNow.AddHours(-24);
            
            var recentNotifications = await work.NotificationRepository.FindAsync(n =>
                n.ServiceType == dbUser.ServiceType &&
                n.User_SourceID == dbUser.SourceID &&
                n.TimeStamp >= cutoffTime &&
                n.Success == true);

            // If there are recent notifications, check if the most recent one was a "stream online" type
            var lastNotification = recentNotifications
                .OrderByDescending(n => n.TimeStamp)
                .FirstOrDefault();

            if (lastNotification == null)
                return false;

            // We assume the user was live if there's a recent successful notification
            // and no corresponding offline notification after it
            // This is a heuristic - in a more sophisticated system, you might track
            // the actual live state more explicitly
            return true;
        }

        /// <summary>
        /// Gets the last stream notification for a user
        /// </summary>
        private async Task<StreamNotification?> GetLastStreamNotification(StreamUser dbUser, IUnitOfWork work)
        {
            var notifications = await work.NotificationRepository.FindAsync(n =>
                n.ServiceType == dbUser.ServiceType &&
                n.User_SourceID == dbUser.SourceID &&
                n.Success == true);

            return notifications
                .OrderByDescending(n => n.TimeStamp)
                .FirstOrDefault();
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
                
                // Create a basic TwitchStream for the offline event using the parameterless constructor
                var offlineStream = new TwitchStream
                {
                    Id = lastNotification.Stream_SourceID ?? "",
                    UserId = user.Id,
                    User = user,
                    GameId = game.Id,
                    Game = game,
                    Title = "Stream Offline", // Generic title for offline event
                    StartTime = lastNotification.TimeStamp, // Use notification time as start time
                    StreamURL = user.ProfileURL,
                    ThumbnailURL = "",
                    ServiceType = _monitor.ServiceType,
                    BaseURL = _monitor.BaseURL
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