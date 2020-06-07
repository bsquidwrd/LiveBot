using Discord;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using System.Globalization;

namespace LiveBot.Discord.Helpers
{
    public static class NotificationHelpers
    {
        public static string EscapeSpecialDiscordCharacters(string input)
        {
            return input
                .Replace("_", "\\_")
                .Replace("*", "\\*")
                .Replace("~", "\\~")
                .Replace("`", "\\`");
        }

        /// <summary>
        /// Formats a notification string with the necessary parameters
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string GetNotificationMessage(ILiveBotStream stream, StreamSubscription subscription)
        {
            // TODO: Implement Role mention and handling for GetNotificationMessage.
            // Change input to ILiveBotNotificationSettings ? Some sort of Model to store notification
            // setup for a given Stream and Discord Server then utilize here instead of given message?
            return subscription.Message
                .Replace("{Name}", EscapeSpecialDiscordCharacters(stream.User.DisplayName), ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{Username}", EscapeSpecialDiscordCharacters(stream.User.DisplayName), ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{Game}", EscapeSpecialDiscordCharacters(stream.Game.Name), ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{Title}", EscapeSpecialDiscordCharacters(stream.Title), ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{URL}", EscapeSpecialDiscordCharacters(stream.GetStreamURL()), ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{Role}", MentionUtils.MentionRole(subscription.Role.DiscordId), ignoreCase: true, culture: CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Generates a Discord Embed for the given <paramref name="stream"/>
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>Discord Embed with Stream Information</returns>
        public static Embed GetStreamEmbed(ILiveBotStream stream)
        {
            // Build the Author of the Embed
            EmbedAuthorBuilder authorBuilder = new EmbedAuthorBuilder();
            authorBuilder.WithName(stream.User.DisplayName);
            authorBuilder.WithIconUrl(stream.User.AvatarURL);
            authorBuilder.WithUrl(stream.User.GetProfileURL());

            // Build the Footer of the Embed
            EmbedFooterBuilder footerBuilder = new EmbedFooterBuilder();
            footerBuilder.WithText("Stream start time");

            // Add Basic information to EmbedBuilder
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithAuthor(authorBuilder);
            builder.WithFooter(footerBuilder);
            builder.WithTimestamp(stream.StartTime);
            builder.WithDescription(stream.Title);
            builder.WithUrl(stream.GetStreamURL());
            builder.WithThumbnailUrl(stream.User.AvatarURL);
            builder.WithColor(Color.DarkPurple);

            // Add Game field
            EmbedFieldBuilder gameBuilder = new EmbedFieldBuilder();
            gameBuilder.WithIsInline(true);
            gameBuilder.WithName("Game");
            gameBuilder.WithValue(stream.Game.Name);
            builder.AddField(gameBuilder);

            // Add Stream URL field
            EmbedFieldBuilder streamURLField = new EmbedFieldBuilder();
            streamURLField.WithIsInline(true);
            streamURLField.WithName("Stream");
            streamURLField.WithValue(stream.GetStreamURL());
            builder.AddField(streamURLField);

            return builder.Build();
        }
    }
}