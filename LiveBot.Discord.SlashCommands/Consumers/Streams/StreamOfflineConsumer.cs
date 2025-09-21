using Discord;
using Discord.Net;
using Discord.WebSocket;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Streams;
using MassTransit;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace LiveBot.Discord.SlashCommands.Consumers.Streams
{
    public class StreamOfflineConsumer : IConsumer<IStreamOffline>
    {
        private readonly DiscordShardedClient _client;
        private readonly IUnitOfWork _work;
        private readonly IBus _bus;
        private readonly ILogger<StreamOfflineConsumer> _logger;

        public StreamOfflineConsumer(DiscordShardedClient client, IUnitOfWorkFactory factory, IBus bus, ILogger<StreamOfflineConsumer> logger)
        {
            _client = client;
            _work = factory.Create();
            _bus = bus;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IStreamOffline> context)
        {
            var stream = context.Message.Stream;
            var user = stream.User;

            var streamUser = await _work.UserRepository.SingleOrDefaultAsync(i => i.ServiceType == stream.ServiceType && i.SourceID == user.Id);
            var streamSubscriptions = await _work.SubscriptionRepository.FindAsync(i => i.User == streamUser);

            if (!streamSubscriptions.Any())
                return;

            foreach (var subscription in streamSubscriptions)
            {
                if (subscription.DiscordGuild == null || subscription.DiscordChannel == null)
                    continue;

                Expression<Func<StreamNotification, bool>> previousNotificationPredicate = (i =>
                    i.ServiceType == subscription.User.ServiceType &&
                    i.User_SourceID == subscription.User.SourceID &&
                    i.DiscordGuild_DiscordId == subscription.DiscordGuild.DiscordId &&
                    i.DiscordChannel_DiscordId == subscription.DiscordChannel.DiscordId &&
                    i.DiscordMessage_DiscordId != null &&
                    i.Stream_SourceID == stream.Id &&
                    i.Success == true
                );
                var lastNotification = await _work.NotificationRepository.SingleOrDefaultAsync(previousNotificationPredicate);

                if (lastNotification == null || lastNotification.DiscordMessage_DiscordId == null)
                    continue;

                SocketGuild? guild = null;
                try
                {
                    guild = _client.GetGuild(subscription.DiscordGuild.DiscordId);
                }
                catch (HttpException ex)
                {
                    if (ex.DiscordCode == DiscordErrorCode.UnknownGuild || ex.DiscordCode == DiscordErrorCode.InvalidGuild)
                        continue;
                    throw;
                }

                if (guild == null)
                    continue;

                SocketTextChannel? channel = null;
                try
                {
                    channel = guild.GetTextChannel(subscription.DiscordChannel.DiscordId);
                }
                catch (HttpException ex)
                {
                    if (ex.DiscordCode == DiscordErrorCode.UnknownChannel)
                        continue;
                    throw;
                }

                if (channel == null)
                    continue;

                async Task RemoveSubscriptionAsync()
                {
                    _logger.LogInformation("Removing Stream Subscription for {Username} on {ServiceType} because missing permissions in {GuildId} {ChannelId} - {SubscriptionId}",
                        streamUser.Username,
                        stream.ServiceType,
                        subscription.DiscordGuild.DiscordId,
                        subscription.DiscordChannel.DiscordId,
                        subscription.Id);

                    var rolesToMention = await _work.RoleToMentionRepository.FindAsync(i => i.StreamSubscription == subscription);
                    foreach (var roleToMention in rolesToMention)
                        await _work.RoleToMentionRepository.RemoveAsync(roleToMention.Id);

                    await _work.SubscriptionRepository.RemoveAsync(subscription.Id);
                }

                IMessage? message = null;
                try
                {
                    message = await channel.GetMessageAsync((ulong)lastNotification.DiscordMessage_DiscordId);
                }
                catch (HttpException ex)
                {
                    if (ex.DiscordCode == DiscordErrorCode.UnknownMessage)
                    {
                        lastNotification.DiscordMessage_DiscordId = null;
                        lastNotification.Success = false;
                        lastNotification.LogMessage = $"Notification message missing when marking offline at {DateTime.UtcNow:o}";
                        await _work.NotificationRepository.UpdateAsync(lastNotification);
                        continue;
                    }

                    if (ex.DiscordCode == DiscordErrorCode.MissingPermissions || ex.DiscordCode == DiscordErrorCode.InsufficientPermissions)
                    {
                        await RemoveSubscriptionAsync();
                        continue;
                    }

                    throw;
                }

                if (message == null || message.Author?.Id != _client.CurrentUser.Id || message is not SocketUserMessage)
                {
                    lastNotification.DiscordMessage_DiscordId = null;
                    lastNotification.Success = false;
                    lastNotification.LogMessage = $"Notification message inaccessible when marking offline at {DateTime.UtcNow:o}";
                    await _work.NotificationRepository.UpdateAsync(lastNotification);
                    continue;
                }

                var embed = message.Embeds.FirstOrDefault();
                var embedBuilder = embed?.ToEmbedBuilder() ?? new EmbedBuilder();
                embedBuilder.WithColor(Color.LightGrey);

                var offlineAt = DateTime.UtcNow;
                var relativeTimestamp = TimestampTag.FromDateTime(offlineAt, TimestampTagStyles.Relative);
                var absoluteTimestamp = TimestampTag.FromDateTime(offlineAt, TimestampTagStyles.LongDateTime);
                var statusMessage = $"Offline {relativeTimestamp} ({absoluteTimestamp})";

                var statusIndex = embedBuilder.Fields.FindIndex(field => field.Name.Equals("Status", StringComparison.InvariantCultureIgnoreCase));
                if (statusIndex >= 0)
                {
                    embedBuilder.Fields[statusIndex].WithValue(statusMessage).WithIsInline(false);
                }
                else
                {
                    embedBuilder.AddField(name: "Status", value: statusMessage, inline: false);
                }

                try
                {
                    await channel.ModifyMessageAsync(message.Id, properties =>
                    {
                        properties.Embed = embedBuilder.Build();
                    });
                }
                catch (HttpException ex)
                {
                    if (ex.DiscordCode == DiscordErrorCode.MissingPermissions || ex.DiscordCode == DiscordErrorCode.InsufficientPermissions)
                    {
                        await RemoveSubscriptionAsync();
                        continue;
                    }

                    throw;
                }

                lastNotification.LogMessage = $"Marked offline at {offlineAt:o}";
                await _work.NotificationRepository.UpdateAsync(lastNotification);
            }
        }

    }
}
