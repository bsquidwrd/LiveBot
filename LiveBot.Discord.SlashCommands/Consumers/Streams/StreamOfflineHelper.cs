using System.Linq.Expressions;
using Discord;
using LiveBot.Core.Repository.Models.Streams;

namespace LiveBot.Discord.SlashCommands.Consumers.Streams
{
    /// <summary>
    /// Helper class for stream offline operations
    /// </summary>
    public static class StreamOfflineHelper
    {
        /// <summary>
        /// Creates the predicate for finding previous notifications for a stream
        /// </summary>
        public static Expression<Func<StreamNotification, bool>> CreatePreviousNotificationPredicate(
            StreamSubscription subscription,
            StreamUser streamUser,
            string streamId)
        {
            return i =>
                i.ServiceType == subscription.User.ServiceType &&
                i.User_SourceID == subscription.User.SourceID &&
                i.DiscordGuild_DiscordId == subscription.DiscordGuild.DiscordId &&
                i.DiscordChannel_DiscordId == subscription.DiscordChannel.DiscordId &&
                i.DiscordMessage_DiscordId != null &&
                i.Stream_SourceID == streamId &&
                i.Success == true;
        }

        /// <summary>
        /// Creates or updates the offline status field in an embed
        /// </summary>
        public static EmbedBuilder UpdateEmbedWithOfflineStatus(IEmbed? originalEmbed)
        {
            var embedBuilder = originalEmbed?.ToEmbedBuilder() ?? new EmbedBuilder();
            embedBuilder.WithColor(Color.LightGrey);

            var offlineAt = DateTime.UtcNow;
            var relativeTimestamp = TimestampTag.FromDateTime(offlineAt, TimestampTagStyles.Relative);
            var absoluteTimestamp = TimestampTag.FromDateTime(offlineAt, TimestampTagStyles.LongDateTime);
            var statusMessage = $"Offline {relativeTimestamp} ({absoluteTimestamp})";

            var statusIndex = embedBuilder.Fields.FindIndex(field =>
                field.Name.Equals("Status", StringComparison.InvariantCultureIgnoreCase));

            if (statusIndex >= 0)
            {
                embedBuilder.Fields[statusIndex].WithValue(statusMessage).WithIsInline(false);
            }
            else
            {
                embedBuilder.AddField(name: "Status", value: statusMessage, inline: false);
            }

            return embedBuilder;
        }

        /// <summary>
        /// Creates a log message for successful offline marking
        /// </summary>
        public static string CreateOfflineLogMessage()
        {
            return $"Marked offline at {DateTime.UtcNow:o}";
        }

        /// <summary>
        /// Creates a removal reason message for logging
        /// </summary>
        public static string CreateRemovalReason(StreamUser streamUser, string serviceType, ulong guildId, ulong channelId, int subscriptionId)
        {
            return $"Removing Stream Subscription for {streamUser.Username} on {serviceType} because missing permissions in {guildId} {channelId} - {subscriptionId}";
        }
    }
}