using Discord;
using Discord.Net;
using Discord.WebSocket;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Streams;
using MassTransit;
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
                if (!subscription.DiscordGuild.IsInBeta)
                    continue;

                if (subscription.DiscordGuild.DiscordId != 225471771355250688)
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

                if (lastNotification == null)
                    continue;
                if (lastNotification.DiscordMessage_DiscordId == null)
                    continue;

                // Try to get the guild
                SocketGuild? guild = null;
                try
                {
                    guild = _client.GetGuild(subscription.DiscordGuild.DiscordId);
                }
                catch (HttpException ex)
                {
                    // If it's an unknown/invalid guild, just continue on
                    if (
                        ex.DiscordCode == DiscordErrorCode.UnknownGuild ||
                        ex.DiscordCode == DiscordErrorCode.InvalidGuild
                    )
                        continue;
                    // Throw any error that wasn't expected
                    throw;
                }
                if (guild == null)
                    continue;

                // Try to get the channel
                SocketTextChannel? channel = null;
                try
                {
                    channel = guild.GetTextChannel(subscription.DiscordChannel.DiscordId);
                }
                catch (HttpException ex)
                {
                    // If it's an unknown channel, just continue on
                    if (ex.DiscordCode == DiscordErrorCode.UnknownChannel)
                        continue;
                    throw;
                }
                if (channel == null)
                    continue;

                // Try to get the message
                IMessage? message = null;
                try
                {
                    message = await channel.GetMessageAsync((ulong)lastNotification.DiscordMessage_DiscordId);
                }
                catch (HttpException ex)
                {
                    // If it's an unknown message, just continue on
                    if (ex.DiscordCode == DiscordErrorCode.UnknownMessage)
                        continue;
                    throw;
                }

                if (message == null || message?.Author?.Id != _client.CurrentUser.Id || message is not SocketUserMessage)
                    continue;

                var embed = message.Embeds.FirstOrDefault();
                if (embed == null)
                    continue;

                var newEmbed = embed.ToEmbedBuilder();
                newEmbed.WithColor(Color.LightGrey);

                var offlineTimestamp = TimestampTag.FromDateTime(DateTime.UtcNow, TimestampTagStyles.Relative);
                var statusMessage = $"Offline {offlineTimestamp}";
                var statusField = newEmbed.Fields.Where(i => i.Name.Equals("Status", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

                if (statusField == null)
                    newEmbed.AddField(name: "Status", value: statusMessage, inline: false);
                else
                    statusField.WithValue(statusMessage).WithIsInline(false);

                await channel.ModifyMessageAsync(messageId: message.Id, i =>
                {
                    i.Embed = newEmbed.Build();
                });
            }
        }
    }
}
