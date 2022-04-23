using Discord;
using Discord.WebSocket;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Discord;
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

        public static string FormatNotificationMessage(string message, IEnumerable<IRole> roles, ILiveBotStream stream, ILiveBotUser user, ILiveBotGame game)
        {
            var roleStrings = new List<string>();
            if (roles.Any())
            {
                roles = roles.OrderBy(i => i.Name);
                foreach (var role in roles)
                {
                    if (role.Name.Equals("@everyone", StringComparison.CurrentCulture))
                        roleStrings.Add("@everyone");
                    else if (role.Name.Equals("@here", StringComparison.CurrentCulture))
                        roleStrings.Add("@here");
                    else
                        roleStrings.Add(role.Mention);
                }
            }
            else
            {
                roleStrings.Add("");
            }
            return message
                .Replace("{Name}", EscapeSpecialDiscordCharacters(user.DisplayName), ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{Username}", EscapeSpecialDiscordCharacters(user.DisplayName), ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{Game}", EscapeSpecialDiscordCharacters(game.Name), ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{Title}", EscapeSpecialDiscordCharacters(stream.Title), ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{URL}", Format.EscapeUrl(stream.StreamURL) ?? "", ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{Role}", String.Join(" ", roleStrings), ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Trim();
        }

        /// <summary>
        /// Formats a notification string with the necessary parameters
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="subscription"></param>
        /// <param name="user"></param>
        /// <param name="game"></param>
        /// <returns></returns>
        public static string GetNotificationMessage(SocketGuild guild, ILiveBotStream stream, StreamSubscription subscription, ILiveBotUser? user = null, ILiveBotGame? game = null)
        {
            var RoleMentions = new List<SocketRole>();
            if (subscription.RolesToMention.Any())
                RoleMentions = subscription.RolesToMention.Select(i => guild.GetRole(i.DiscordRoleId)).ToList();

            var tempUser = user ?? stream.User;
            var tempGame = game ?? stream.Game;

            return FormatNotificationMessage(message: subscription.Message, roles: RoleMentions, stream: stream, user: tempUser, game: tempGame);
        }

        /// <summary>
        /// Formats a notification string with the necessary parameters
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="config"></param>
        /// <param name="user"></param>
        /// <param name="game"></param>
        /// <returns></returns>
        public static string GetNotificationMessage(SocketGuild guild, ILiveBotStream stream, DiscordGuildConfig config, ILiveBotUser user, ILiveBotGame game)
        {
            var RoleMentions = new List<SocketRole>();
            if (config.MentionRoleDiscordId.HasValue)
                RoleMentions.Add(guild.GetRole(config.MentionRoleDiscordId.Value));

            return FormatNotificationMessage(message: config.Message ?? Defaults.NotificationMessage, roles: RoleMentions, stream: stream, user: user, game: game);
        }

        /// <summary>
        /// Generates a Discord Embed for the given <paramref name="stream"/>
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>Discord Embed with Stream Information</returns>
        public static Embed GetStreamEmbed(ILiveBotStream stream, ILiveBotUser user, ILiveBotGame game)
        {
            // Build the Author of the Embed
            var authorBuilder = new EmbedAuthorBuilder()
                .WithName(user.DisplayName)
                .WithIconUrl(user.AvatarURL)
                .WithUrl(user.ProfileURL);

            // Build the Footer of the Embed
            var footerBuilder = new EmbedFooterBuilder()
                .WithText("Stream start time");

            // Add Basic information to EmbedBuilder
            var builder = new EmbedBuilder()
                .WithColor(stream.ServiceType.GetAlertColor())
                .WithAuthor(authorBuilder)
                .WithFooter(footerBuilder)
                .WithTimestamp(stream.StartTime)
                .WithDescription(EscapeSpecialDiscordCharacters(stream.Title))
                .WithUrl(stream.StreamURL)
                .WithThumbnailUrl(user.AvatarURL);

            // Add Game field
            builder.AddField(name: "Game", value: game.Name, inline: true);

            // Add Stream URL field
            builder.AddField(name: "Stream", value: stream.StreamURL, inline: true);

            // Add Status Field
            //builder.AddField(name: "Status", value: "", inline: false);

            return builder.Build();
        }
    }
}