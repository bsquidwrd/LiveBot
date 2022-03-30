using Discord;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;
using System.Globalization;

namespace LiveBot.Discord.SlashCommands.Helpers
{
    public static class NotificationHelpers
    {
        public static string EscapeSpecialDiscordCharacters(string input)
        {
            return Format.Sanitize(input);
        }

        /// <summary>
        /// Formats a notification string with the necessary parameters
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string GetNotificationMessage(ILiveBotStream stream, StreamSubscription subscription, ILiveBotUser? user = null, ILiveBotGame? game = null)
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
                .Replace("{URL}", Format.EscapeUrl(stream.StreamURL) ?? "", ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{Role}", RoleMention, ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Trim();
        }

        /// <summary>
        /// Generates a Discord Embed for the given <paramref name="stream"/>
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>Discord Embed with Stream Information</returns>
        public static Embed GetStreamEmbed(ILiveBotStream stream, ILiveBotUser user, ILiveBotGame game)
        {
            // Build the Author of the Embed
            var authorBuilder = new EmbedAuthorBuilder();
            authorBuilder.WithName(user.DisplayName);
            authorBuilder.WithIconUrl(user.AvatarURL);
            authorBuilder.WithUrl(user.ProfileURL);

            // Build the Footer of the Embed
            var footerBuilder = new EmbedFooterBuilder();
            footerBuilder.WithText("Stream start time");

            // Add Basic information to EmbedBuilder
            var builder = new EmbedBuilder();
            builder.WithColor(stream.ServiceType.GetAlertColor());

            builder.WithAuthor(authorBuilder);
            builder.WithFooter(footerBuilder);

            builder.WithTimestamp(stream.StartTime);
            builder.WithDescription(EscapeSpecialDiscordCharacters(stream.Title));
            builder.WithUrl(stream.StreamURL);
            builder.WithThumbnailUrl(user.AvatarURL);

            // Add Status Field
            //EmbedFieldBuilder statusBuilder = new EmbedFieldBuilder();
            //statusBuilder.WithIsInline(false);
            //statusBuilder.WithName("Status");
            //statusBuilder.WithValue("");
            //builder.AddField(statusBuilder);

            // Add Game field
            var gameBuilder = new EmbedFieldBuilder();
            gameBuilder.WithIsInline(true);
            gameBuilder.WithName("Game");
            gameBuilder.WithValue(game.Name);
            builder.AddField(gameBuilder);

            // Add Stream URL field
            var streamURLField = new EmbedFieldBuilder();
            streamURLField.WithIsInline(true);
            streamURLField.WithName("Stream");
            streamURLField.WithValue(stream.StreamURL);
            builder.AddField(streamURLField);

            return builder.Build();
        }
    }
}