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
        public static string GetNotificationMessage(ILiveBotStream stream, StreamSubscription subscription, ILiveBotUser user = null, ILiveBotGame game = null)
        {
            string RoleMention = "";
            if (subscription.DiscordRole != null)
            {
                if (subscription.DiscordRole.Name == "@everyone")
                {
                    RoleMention = "@everyone";
                }
                else
                {
                    RoleMention = MentionUtils.MentionRole(subscription.DiscordRole.DiscordId);
                }
            }

            var tempUser = user ?? stream.User;
            var tempGame = game ?? stream.Game;

            return subscription.Message
                .Replace("{Name}", EscapeSpecialDiscordCharacters(tempUser.DisplayName), ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{Username}", EscapeSpecialDiscordCharacters(tempUser.DisplayName), ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{Game}", EscapeSpecialDiscordCharacters(tempGame.Name), ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{Title}", EscapeSpecialDiscordCharacters(stream.Title), ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{URL}", EscapeSpecialDiscordCharacters(stream.StreamURL), ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{Role}", RoleMention, ignoreCase: true, culture: CultureInfo.CurrentCulture);
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
            authorBuilder.WithUrl(stream.User.ProfileURL);

            // Build the Footer of the Embed
            EmbedFooterBuilder footerBuilder = new EmbedFooterBuilder();
            footerBuilder.WithText("Stream start time");

            // Add Basic information to EmbedBuilder
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(Color.DarkPurple);

            builder.WithAuthor(authorBuilder);
            builder.WithFooter(footerBuilder);

            builder.WithTimestamp(stream.StartTime);
            builder.WithDescription(stream.Title);
            builder.WithUrl(stream.StreamURL);
            builder.WithThumbnailUrl(stream.User.AvatarURL);

            // Add Status Field
            //EmbedFieldBuilder statusBuilder = new EmbedFieldBuilder();
            //statusBuilder.WithIsInline(false);
            //statusBuilder.WithName("Status");
            //statusBuilder.WithValue("");
            //builder.AddField(statusBuilder);

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
            streamURLField.WithValue(stream.StreamURL);
            builder.AddField(streamURLField);

            return builder.Build();
        }
    }
}