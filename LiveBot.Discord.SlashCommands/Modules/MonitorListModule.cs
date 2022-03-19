using Discord;
using Discord.Interactions;
using Discord.Rest;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;
using LiveBot.Discord.SlashCommands.Attributes;

namespace LiveBot.Discord.SlashCommands.Modules
{
    public partial class MonitorModule : RestInteractionModuleBase<RestInteractionContext>
    {
        /// <summary>
        /// List all stream monitors in the Guild
        /// </summary>
        /// <returns></returns>
        [SlashCommand(name: "list", description: "List all stream monitors")]
        public async Task ListStreamMonitorAsync()
        {
            try
            {
                await DeferAsync(ephemeral: true);
            }
            catch { }

            var subscriptions = await _work.SubscriptionRepository.FindAsync(i => i.DiscordGuild.DiscordId == Context.Guild.Id);
            subscriptions = subscriptions.OrderBy(i => i.User.Username);

            if (!subscriptions.Any())
            {
                await FollowupAsync("There are no subscriptions for this server!", ephemeral: true);
                return;
            }

            var subscription = subscriptions.First();

            var subscriptionEmbed = MonitorListUtils.GetSubscriptionEmbed(currentSpot: 0, subscription: subscription, subscriptionCount: subscriptions.Count());
            var messageComponents = MonitorListUtils.GetSubscriptionComponents(subscription: subscription, previousSpot: 0, nextSpot: 1);

            await FollowupAsync(text: "", ephemeral: true, embed: subscriptionEmbed, components: messageComponents);
        }
    }

    public class MonitorListModule : RestInteractionModuleBase<RestInteractionContext>
    {
        private readonly ILogger<MonitorListModule> _logger;
        private readonly IUnitOfWork _work;

        public MonitorListModule(ILogger<MonitorListModule> logger, IUnitOfWorkFactory factory)
        {
            _logger = logger;
            _work = factory.Create();
        }

        [RequireBotManager]
        [ComponentInteraction(customId: "monitorlist:*")]
        public async Task MonitorListAsync(int currentSpot)
        {
            if (Context.Interaction is RestMessageComponent component)
            {
                var subscriptions = await _work.SubscriptionRepository.FindAsync(i => i.DiscordGuild.DiscordId == Context.Guild.Id);
                subscriptions = subscriptions.OrderBy(i => i.User.Username);

                if (!subscriptions.Any())
                {
                    var zeroCountResponse = component.Update(x =>
                    {
                        x.Content = "There are no subscriptions for this server!";
                        x.Embed = null;
                        x.Components = null;
                    });
                    await Context.InteractionResponseCallback(zeroCountResponse);
                    return;
                }

                if (currentSpot > subscriptions.Count() - 1)
                    currentSpot = 0;

                var subscription = subscriptions.ToList()[currentSpot];

                int previousSpot = currentSpot - 1;
                int nextSpot = currentSpot + 1;

                if (previousSpot < 0)
                    previousSpot = 0;

                if (nextSpot > subscriptions.Count())
                    nextSpot = 0;

                var subscriptionEmbed = MonitorListUtils.GetSubscriptionEmbed(currentSpot: currentSpot, subscription: subscription, subscriptionCount: subscriptions.Count());
                var messageComponents = MonitorListUtils.GetSubscriptionComponents(subscription, previousSpot: previousSpot, nextSpot: nextSpot);

                var updateResponse = component.Update(x =>
                {
                    x.Content = "";
                    x.Embed = subscriptionEmbed;
                    x.Components = messageComponents;
                });
                await Context.InteractionResponseCallback(updateResponse);
            }
        }

        [RequireBotManager]
        [ComponentInteraction(customId: "monitordelete:*")]
        public async Task MonitorDeleteAsync(long subscriptionId)
        {
            try
            {
                await DeferAsync(ephemeral: true);
            }
            catch { }
            var subscription = await _work.SubscriptionRepository.GetAsync(subscriptionId);
            var displayName = subscription.User.DisplayName;
            await _work.SubscriptionRepository.RemoveAsync(subscription.Id);
            await FollowupAsync(text: $"Monitor for {Format.Bold(displayName)} has been deleted", ephemeral: true);
        }
    }

    internal static class MonitorListUtils
    {
        internal static MessageComponent GetSubscriptionComponents(StreamSubscription subscription, int previousSpot = 0, int nextSpot = 1)
        {
            return new ComponentBuilder()
                .WithButton(label: "Back", customId: $"monitorlist:{previousSpot}", style: ButtonStyle.Primary)
                .WithButton(label: "Next", customId: $"monitorlist:{nextSpot}", style: ButtonStyle.Primary)
                .WithButton(label: "Delete", customId: $"monitordelete:{subscription.Id}", style: ButtonStyle.Danger, emote: new Emoji("\uD83D\uDDD1"))
                .Build();
        }

        internal static Embed GetSubscriptionEmbed(StreamSubscription subscription, int subscriptionCount, int currentSpot = 0)
        {
            // Build the Footer of the Embed
            var footerBuilder = new EmbedFooterBuilder()
                .WithText($"Page {currentSpot + 1}/{subscriptionCount}");

            // Build the Author of the Embed
            var authorBuilder = new EmbedAuthorBuilder()
                .WithName(subscription.User.DisplayName)
                .WithIconUrl(subscription.User.AvatarURL)
                .WithUrl(subscription.User.ProfileURL);

            // Add Basic information to EmbedBuilder
            var builder = new EmbedBuilder()
                .WithColor(subscription.User.ServiceType.GetAlertColor())
                .WithAuthor(authorBuilder)
                .WithThumbnailUrl(subscription.User.AvatarURL)
                .WithFooter(footerBuilder);

            // Add Role field
            var discordRole = subscription.DiscordRole == null ? "none" : MentionUtils.MentionRole(subscription.DiscordRole.DiscordId);
            var roleBuilder = new EmbedFieldBuilder()
                .WithIsInline(true)
                .WithName("Role")
                .WithValue(discordRole);
            builder.AddField(roleBuilder);

            // Add Channel field
            var channelField = new EmbedFieldBuilder()
                .WithIsInline(true)
                .WithName("Channel")
                .WithValue(MentionUtils.MentionChannel(subscription.DiscordChannel.DiscordId));
            builder.AddField(channelField);

            // Add Message field
            var messageField = new EmbedFieldBuilder()
                .WithIsInline(false)
                .WithName("Message")
                .WithValue(subscription.Message);
            builder.AddField(messageField);

            return builder.Build();
        }
    }
}