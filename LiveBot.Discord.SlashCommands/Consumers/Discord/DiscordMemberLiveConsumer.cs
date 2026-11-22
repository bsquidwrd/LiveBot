using System.Linq.Expressions;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;
using LiveBot.Discord.SlashCommands.Helpers;
using LiveBot.Discord.SlashCommands.Models;
using MassTransit;

namespace LiveBot.Discord.SlashCommands.Consumers.Discord
{
    public class DiscordMemberLiveConsumer : IConsumer<IDiscordMemberLive>
    {
        private readonly DiscordShardedClient _client;
        private readonly IUnitOfWork _work;
        private readonly IEnumerable<ILiveBotMonitor> _monitors;
        private readonly ILogger<DiscordMemberLiveConsumer> _logger;

        public DiscordMemberLiveConsumer(DiscordShardedClient client, IUnitOfWork work, IEnumerable<ILiveBotMonitor> monitors, ILogger<DiscordMemberLiveConsumer> logger)
        {
            _client = client;
            _work = work;
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

            // SocketUser.MutualGuilds only covers the shard that owns the user, so walk every shard.
            var mutualGuilds = new List<SocketGuild>();
            var seenGuildIds = new HashSet<ulong>();
            foreach (DiscordSocketClient shard in _client.Shards)
            {
                foreach (SocketGuild guild in shard.Guilds)
                {
                    if (guild.GetUser(user.Id) != null && seenGuildIds.Add(guild.Id))
                        mutualGuilds.Add(guild);
                }
            }

            foreach (var guild in mutualGuilds)
            {
                if (guild?.Id == null) continue;

                var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == guild.Id);
                if (discordGuild == null) continue;

                // If they don't have any of the proper settings set, ignore
                var guildConfig = await _work.GuildConfigRepository.SingleOrDefaultAsync(i => i.DiscordGuild.DiscordId == guild.Id);
                if (guildConfig == null) continue;
                if (guildConfig.MonitorRoleDiscordId == null || guildConfig.DiscordChannel == null)
                    continue;

                if (String.IsNullOrWhiteSpace(guildConfig.Message))
                {
                    guildConfig.Message = Defaults.NotificationMessage;
                    await _work.GuildConfigRepository.UpdateAsync(guildConfig);
                }

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

                var discordGame = new DiscordGame(serviceType, userGame);
                var discordUser = new DiscordUser(serviceType, guildUser);
                var discordStream = new DiscordStream(serviceType, message, discordUser, discordGame);

                var embed = NotificationHelpers.GetStreamEmbed(discordStream, discordUser, discordGame);
                var liveMessage = NotificationHelpers.GetNotificationMessage(guild: guild, stream: discordStream, config: guildConfig, user: discordUser, game: discordGame);

                // Rows starting after LiveTime also match, same as the original in-memory check.
                var previousCutoff = message.LiveTime.AddMinutes(-60);
                Expression<Func<StreamNotification, bool>> previousNotificationPredicate = (i =>
                        i.User_SourceID == guildUser.Id.ToString()
                        && i.DiscordGuild_DiscordId == guild.Id
                        && i.DiscordChannel_DiscordId == channel.Id
                        && i.Success == true
                        && i.Stream_StartTime >= previousCutoff
                    );
                var hasPreviousNotification = await _work.NotificationRepository.AnyAsync(previousNotificationPredicate);

                var guildRole = guildConfig.MentionRoleDiscordId != null ? guild.GetRole((ulong)guildConfig.MentionRoleDiscordId) : null;
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
                    DiscordRole_Name = guildRole?.Name ?? "none",
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
                if (hasPreviousNotification)
                    newStreamNotification.Success = true;

                await _work.NotificationRepository.AddOrUpdateAsync(newStreamNotification, notificationPredicate);
                StreamNotification streamNotification = await _work.NotificationRepository.SingleOrDefaultAsync(notificationPredicate);

                // If the current notification was marked as a success, end processing
                if (newStreamNotification.Success == true)
                    continue;

                // Claimed before posting so a message retry after a successful post finds this row
                // through the previous-notification check and skips it instead of posting again.
                streamNotification.Success = true;
                streamNotification.LogMessage = $"Sending at {DateTime.UtcNow:o}";
                await _work.NotificationRepository.UpdateAsync(streamNotification);

                IUserMessage discordMessage;
                try
                {
                    discordMessage = await channel.SendMessageAsync(text: liveMessage, embed: embed);
                }
                catch (Exception ex)
                {
                    streamNotification.Success = false;
                    streamNotification.LogMessage = $"Error sending notification at {DateTime.UtcNow:o}: {ex.Message}";
                    await _work.NotificationRepository.UpdateAsync(streamNotification);

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
                            message: "Error sending notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId} {@RoleIds}, {Message} {IsFromRole}, {MillisecondsToPost}",
                            streamNotification.Id,
                            streamNotification.ServiceType,
                            streamNotification.User_Username,
                            streamNotification.DiscordGuild_DiscordId.ToString(),
                            streamNotification.DiscordChannel_DiscordId.ToString(),
                            streamNotification.DiscordRole_DiscordId?.ToString().Split(","),
                            streamNotification.Message,
                            true,
                            (DateTime.UtcNow - streamNotification.Stream_StartTime).TotalMilliseconds
                        );
                    }

                    continue;
                }

                streamNotification.DiscordMessage_DiscordId = discordMessage.Id;
                streamNotification.LogMessage = "From Discord Role Monitor";
                await _work.NotificationRepository.UpdateAsync(streamNotification);

                _logger.LogInformation(
                    message: "Sent notification for {NotificationId} {ServiceType} {Username} {GuildId} {ChannelId} {@RoleIds}, {Message} {IsFromRole}, {MillisecondsToPost}",
                    streamNotification.Id,
                    streamNotification.ServiceType,
                    streamNotification.User_Username,
                    streamNotification.DiscordGuild_DiscordId.ToString(),
                    streamNotification.DiscordChannel_DiscordId.ToString(),
                    streamNotification.DiscordRole_DiscordId?.ToString().Split(","),
                    streamNotification.Message,
                    true,
                    (DateTime.UtcNow - streamNotification.Stream_StartTime).TotalMilliseconds
                );
            }
        }
    }
}