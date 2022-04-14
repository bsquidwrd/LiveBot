using Discord;
using Discord.Net;
using Discord.WebSocket;
using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;
using MassTransit;
using System.Globalization;
using System.Linq.Expressions;

namespace LiveBot.Discord.SlashCommands.Consumers.Discord
{
    public class DiscordMemberLiveConsumer : IConsumer<IDiscordMemberLive>
    {
        private readonly DiscordShardedClient _client;
        private readonly IUnitOfWork _work;
        private readonly IEnumerable<ILiveBotMonitor> _monitors;
        private readonly ILogger<DiscordMemberLiveConsumer> _logger;

        public DiscordMemberLiveConsumer(DiscordShardedClient client, IUnitOfWorkFactory factory, IEnumerable<ILiveBotMonitor> monitors, ILogger<DiscordMemberLiveConsumer> logger)
        {
            _client = client;
            _work = factory.Create();
            _monitors = monitors;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IDiscordMemberLive> context)
        {
            var message = context.Message;
            var user = _client.GetUser(message.DiscordUserId);
            if (user == null) return;

            // I don't care about bots
            if (user.IsBot)
                return;

            // Check if the updated user has an activity set Also make sure it's a Streaming type of Activity
            StreamingGame? userGame = null;
            foreach (var userActivity in user.Activities)
            {
                if (userActivity.Type == ActivityType.Streaming && userActivity is StreamingGame game)
                {
                    // If there's no URL, then skip
                    if (String.IsNullOrWhiteSpace(game.Url))
                        continue;

                    userGame = game;
                    break;
                }
            }

            // Incase one couldn't be found, skip
            if (userGame == null)
                return;

            foreach (var guild in user.MutualGuilds)
            {
                if (guild == null) continue;

                var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == guild.Id && i.IsInBeta == true);
                if (discordGuild == null) continue;

                // If they don't have any of the proper settings set, ignore
                var guildConfig = await _work.GuildConfigRepository.SingleOrDefaultAsync(i => i.DiscordGuild.DiscordId == guild.Id);
                if (guildConfig == null) continue;
                if (guildConfig.MonitorRoleDiscordId == null || guildConfig.DiscordChannel == null || String.IsNullOrWhiteSpace(guildConfig.Message))
                    continue;

                // Get the guild user
                var guildUser = guild.GetUser(user.Id);
                if (guildUser == null) continue;

                // If the user doesn't have the monitor role, continue
                if (!guildUser.Roles.Any(i => i.Id == guildConfig.MonitorRoleDiscordId)) continue;

                // Get the channel
                var channel = guild.GetTextChannel(guildConfig.DiscordChannel.DiscordId);
                if (channel == null) continue;

                // Attempt to get the color specific to the streaming service
                var monitor = _monitors.Where(i => i.IsValid(message.Url)).FirstOrDefault();
                var serviceType = monitor?.ServiceType ?? (new Uri(message.Url)).ToServiceEnum();
                var alertColor = serviceType.GetAlertColor();

                // Make sure there's not a subscription for this user
                // this prevents duplicate notifications
                if (monitor != null)
                {
                    try
                    {
                        var monitorUser = await monitor.GetUser(profileURL: message.Url);
                        var existingSubscription = await _work.SubscriptionRepository.FindAsync(i =>
                            i.DiscordGuild.DiscordId == guild.Id
                            && i.User.SourceID == monitorUser.Id
                            && i.User.ServiceType == serviceType
                        );
                        if (existingSubscription.Any())
                            continue;
                    }
                    // Do nothing on error, assuming it's because the monitor isn't setup
                    catch (NotImplementedException) { }
                }

                var embed = new EmbedBuilder()
                    .WithColor(color: alertColor)
                    .WithDescription(description: Format.Sanitize(message.GameDetails))
                    .WithAuthor(user: guildUser)
                    .WithFooter(text: "Stream start time")
                    .WithCurrentTimestamp()
                    .WithUrl(url: message.Url)
                    .WithThumbnailUrl(thumbnailUrl: guildUser.GetGuildAvatarUrl() ?? guildUser.GetAvatarUrl() ?? guildUser.GetDefaultAvatarUrl())

                    .AddField(name: "Game", value: message.GameName, inline: true)
                    .AddField(name: "Stream", value: message.Url, inline: true)

                    .Build();

                var roleToMention = "";
                var guildRole = guildConfig.MentionRoleDiscordId != null ? guild.GetRole((ulong)guildConfig.MentionRoleDiscordId) : null;
                if (guildConfig.MentionRoleDiscordId != null)
                    roleToMention = MentionUtils.MentionRole((ulong)guildConfig.MentionRoleDiscordId);

                var liveMessage = guildConfig.Message
                    .Replace("{Name}", Format.Sanitize(guildUser.DisplayName), ignoreCase: true, culture: CultureInfo.InvariantCulture)
                    .Replace("{Username}", Format.Sanitize(guildUser.DisplayName), ignoreCase: true, culture: CultureInfo.InvariantCulture)
                    .Replace("{Game}", Format.Sanitize(message.GameName), ignoreCase: true, culture: CultureInfo.InvariantCulture)
                    .Replace("{Title}", Format.Sanitize(message.GameDetails), ignoreCase: true, culture: CultureInfo.InvariantCulture)
                    .Replace("{URL}", Format.EscapeUrl(message.Url) ?? "", ignoreCase: true, culture: CultureInfo.InvariantCulture)
                    .Replace("{Role}", roleToMention, ignoreCase: true, culture: CultureInfo.InvariantCulture)
                    .Trim();

                Expression<Func<StreamNotification, bool>> previousNotificationPredicate = (i =>
                        i.User_SourceID == guildUser.Id.ToString()
                        && i.DiscordGuild_DiscordId == guild.Id
                        && i.DiscordChannel_DiscordId == channel.Id
                    );
                var previousNotifications = await _work.NotificationRepository.FindAsync(previousNotificationPredicate);
                previousNotifications = previousNotifications.Where(i =>
                        message.LiveTime.Subtract(i.Stream_StartTime).TotalMinutes <= 60 // If within an hour of their last start time
                        && i.Success == true // Only pull Successful notifications
                    );

                var newStreamNotification = new StreamNotification
                {
                    ServiceType = serviceType,
                    Success = false,
                    Message = guildConfig.Message,

                    User_SourceID = guildUser.Id.ToString(),
                    User_Username = guildUser.Username,
                    User_DisplayName = guildUser.DisplayName,
                    User_AvatarURL = guildUser.GetGuildAvatarUrl() ?? guildUser.GetAvatarUrl() ?? guildUser.GetDefaultAvatarUrl(),

                    Stream_Title = message.GameDetails,
                    Stream_StartTime = message.LiveTime,
                    Stream_StreamURL = message.Url,

                    Game_Name = message.GameName,

                    DiscordGuild_DiscordId = guild.Id,
                    DiscordGuild_Name = guild.Name,

                    DiscordChannel_DiscordId = channel.Id,
                    DiscordChannel_Name = channel.Name,

                    DiscordRole_DiscordId = guildRole?.Id ?? 0,
                    DiscordRole_Name = guildRole?.Name ?? "none"
                };

                Expression<Func<StreamNotification, bool>> notificationPredicate = (i =>
                        i.User_SourceID == newStreamNotification.User_SourceID &&
                        i.Stream_SourceID == newStreamNotification.Stream_SourceID &&
                        i.Stream_StartTime == newStreamNotification.Stream_StartTime &&
                        i.DiscordGuild_DiscordId == newStreamNotification.DiscordGuild_DiscordId &&
                        i.DiscordChannel_DiscordId == newStreamNotification.DiscordChannel_DiscordId &&
                        i.Game_SourceID == newStreamNotification.Game_SourceID
                    );

                // If there is already 1 or more notifications that were successful in the past hour
                // mark this current one as a success
                if (previousNotifications.Any())
                    newStreamNotification.Success = true;

                await _work.NotificationRepository.AddOrUpdateAsync(newStreamNotification, notificationPredicate);
                StreamNotification streamNotification = await _work.NotificationRepository.SingleOrDefaultAsync(notificationPredicate);

                // If the current notification was marked as a success, end processing
                if (newStreamNotification.Success == true)
                    continue;

                try
                {
                    var discordMessage = await channel.SendMessageAsync(text: guildConfig.Message, embed: embed);
                    streamNotification.DiscordMessage_DiscordId = discordMessage.Id;
                    streamNotification.Success = true;
                    streamNotification.LogMessage = "From Discord Role Monitor";
                    await _work.NotificationRepository.UpdateAsync(streamNotification);

                    _logger.LogInformation(
                        message: "Sent notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId} {@RoleIds}, {Message} {IsFromRole}",
                        streamNotification.Id,
                        streamNotification.ServiceType,
                        streamNotification.User_Username,
                        streamNotification.DiscordGuild_DiscordId,
                        streamNotification.DiscordChannel_DiscordId,
                        streamNotification.DiscordRole_DiscordId?.ToString().Split(","),
                        streamNotification.Message,
                        true
                    );
                }
                catch (Exception ex)
                {
                    if (ex is HttpException discordError)
                    {
                        // You lack permissions to perform that action
                        if (
                            discordError.DiscordCode == DiscordErrorCode.InsufficientPermissions
                            || discordError.DiscordCode == DiscordErrorCode.MissingPermissions
                        )
                        {
                            // I'm tired of seeing errors for Missing Permissions
                            continue;
                        }
                    }
                    else
                    {
                        _logger.LogError(
                            exception: ex,
                            message: "Error sending notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId} {@RoleIds}, {Message} {IsFromRole}",
                            streamNotification.Id,
                            streamNotification.ServiceType,
                            streamNotification.User_Username,
                            streamNotification.DiscordGuild_DiscordId,
                            streamNotification.DiscordChannel_DiscordId,
                            streamNotification.DiscordRole_DiscordId?.ToString().Split(","),
                            streamNotification.Message,
                            true
                        );
                    }
                }
            }
        }
    }
}