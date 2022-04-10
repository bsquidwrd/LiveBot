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
            var memberLive = context.Message;

            var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == memberLive.DiscordGuildId && i.IsInBeta == true);

            if (discordGuild == null) return;

            // If the Guild ID is not whitelisted, don't do anything This is for Beta testing
            bool isInBeta = discordGuild?.IsInBeta ?? false;
            if (!isInBeta) return;

            var guildConfig = await _work.GuildConfigRepository.SingleOrDefaultAsync(i => i.DiscordGuild.DiscordId == context.Message.DiscordGuildId);

            // If they don't have any of the proper settings set, ignore
            if (guildConfig == null) return;
            if (guildConfig.MonitorRoleDiscordId == null || guildConfig.DiscordChannel == null || guildConfig.Message == null)
                return;

            // Get the guild
            var guild = _client.GetGuild(memberLive.DiscordGuildId);
            if (guild == null) return;

            // Get the guild user
            var user = guild.GetUser(memberLive.DiscordUserId);
            if (user == null) return;

            // If the user doesn't have the monitor role, return
            if (!user.Roles.Any(i => i.Id == guildConfig.MonitorRoleDiscordId)) return;

            // Get the channel
            var channel = guild.GetTextChannel(guildConfig.DiscordChannel.DiscordId);
            if (channel == null) return;

            // Attempt to get the color specific to the streaming service
            var monitor = _monitors.Where(i => i.IsValid(context.Message.Url)).FirstOrDefault();
            var serviceType = monitor?.ServiceType ?? ServiceEnum.None;
            var alertColor = serviceType.GetAlertColor();

            var embed = new EmbedBuilder()
                .WithColor(color: alertColor)
                .WithDescription(description: Format.Sanitize(memberLive.GameDetails))
                .WithAuthor(user: user)
                .WithFooter(text: "Stream start time")
                .WithCurrentTimestamp()
                .WithUrl(url: memberLive.Url)
                .WithThumbnailUrl(thumbnailUrl: user.GetGuildAvatarUrl() ?? user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())

                .AddField(name: "Game", value: memberLive.GameName, inline: true)
                .AddField(name: "Stream", value: memberLive.Url, inline: true)

                .Build();

            var roleToMention = "";
            var guildRole = guildConfig.MentionRoleDiscordId != null ? guild.GetRole((ulong)guildConfig.MentionRoleDiscordId) : null;
            if (guildConfig.MentionRoleDiscordId != null)
                roleToMention = MentionUtils.MentionRole((ulong)guildConfig.MentionRoleDiscordId);

            var message = guildConfig.Message
                .Replace("{Name}", Format.Sanitize(user.DisplayName), ignoreCase: true, culture: CultureInfo.InvariantCulture)
                .Replace("{Username}", Format.Sanitize(user.DisplayName), ignoreCase: true, culture: CultureInfo.InvariantCulture)
                .Replace("{Game}", Format.Sanitize(memberLive.GameName), ignoreCase: true, culture: CultureInfo.InvariantCulture)
                .Replace("{Title}", Format.Sanitize(memberLive.GameDetails), ignoreCase: true, culture: CultureInfo.InvariantCulture)
                .Replace("{URL}", Format.EscapeUrl(memberLive.Url) ?? "", ignoreCase: true, culture: CultureInfo.InvariantCulture)
                .Replace("{Role}", roleToMention, ignoreCase: true, culture: CultureInfo.InvariantCulture)
                .Trim();

            Expression<Func<StreamNotification, bool>> previousNotificationPredicate = (i =>
                    i.User_SourceID == user.Id.ToString()
                    && i.DiscordGuild_DiscordId == guild.Id
                    && i.DiscordChannel_DiscordId == channel.Id
                );
            var previousNotifications = await _work.NotificationRepository.FindAsync(previousNotificationPredicate);
            previousNotifications = previousNotifications.Where(i =>
                    memberLive.LiveTime.Subtract(i.Stream_StartTime).TotalMinutes <= 60 // If within an hour of their last start time
                    && i.Success == true // Only pull Successful notifications
                );

            var newStreamNotification = new StreamNotification
            {
                ServiceType = serviceType,
                Success = false,
                Message = message,

                User_SourceID = user.Id.ToString(),
                User_Username = user.Username,
                User_DisplayName = user.DisplayName,
                User_AvatarURL = user.GetGuildAvatarUrl() ?? user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(),

                Stream_Title = memberLive.GameDetails,
                Stream_StartTime = memberLive.LiveTime,
                Stream_StreamURL = memberLive.Url,

                Game_Name = memberLive.GameName,

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
                return;

            try
            {
                var discordMessage = await channel.SendMessageAsync(text: message, embed: embed);
                streamNotification.DiscordMessage_DiscordId = discordMessage.Id;
                streamNotification.Success = true;
                streamNotification.LogMessage = "From Discord Role Monitor";
                await _work.NotificationRepository.UpdateAsync(streamNotification);
            }
            catch (Exception e)
            {
                if (e is HttpException discordError)
                {
                    // You lack permissions to perform that action
                    if (
                        discordError.DiscordCode == DiscordErrorCode.InsufficientPermissions
                        || discordError.DiscordCode == DiscordErrorCode.MissingPermissions
                    )
                    {
                        // I'm tired of seeing errors for Missing Permissions
                        return;
                    }
                }
                else
                {
                    _logger.LogError("Error sending notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId} {RoleId}, {Message}\n{e}", streamNotification.Id, streamNotification.ServiceType, streamNotification.User_Username, streamNotification.DiscordGuild_DiscordId, streamNotification.DiscordChannel_DiscordId, streamNotification.DiscordRole_DiscordId, streamNotification.Message, e);
                }
            }
        }
    }
}