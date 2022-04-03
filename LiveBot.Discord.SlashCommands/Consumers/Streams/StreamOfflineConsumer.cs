using Discord;
using Discord.Rest;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Streams;
using MassTransit;
using System.Linq.Expressions;

namespace LiveBot.Discord.SlashCommands.Consumers.Streams
{
    public class StreamOfflineConsumer : IConsumer<IStreamOffline>
    {
        private readonly DiscordRestClient _client;
        private readonly IUnitOfWork _work;
        private readonly IBusControl _bus;
        private readonly ILogger<StreamOfflineConsumer> _logger;

        public StreamOfflineConsumer(DiscordRestClient client, IUnitOfWorkFactory factory, IBusControl bus, ILogger<StreamOfflineConsumer> logger)
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

                var guild = await _client.GetGuildAsync(subscription.DiscordGuild.DiscordId);
                var channel = await guild.GetTextChannelAsync(subscription.DiscordChannel.DiscordId);
                var message = await channel.GetMessageAsync((ulong)lastNotification.DiscordMessage_DiscordId);

                if (message.Author.Id != _client.CurrentUser.Id)
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
                    //i.Content = string.IsNullOrWhiteSpace(message.Content) ? "" : $"{Format.Bold("[OFFLINE]")} {message.Content}";
                    i.Embed = newEmbed.Build();
                });
            }
        }
    }
}