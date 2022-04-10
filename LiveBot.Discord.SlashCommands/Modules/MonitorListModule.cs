using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;
using LiveBot.Discord.SlashCommands.Attributes;

namespace LiveBot.Discord.SlashCommands.Modules
{
    public partial class MonitorModule : InteractionModuleBase<ShardedInteractionContext>
    {
        #region list command

        /// <summary>
        /// List all stream monitors in the Guild
        /// </summary>
        /// <returns></returns>
        [SlashCommand(name: "list", description: "List all stream monitors")]
        public async Task ListStreamMonitorAsync()
        {
            var subscriptions = await _work.SubscriptionRepository.FindAsync(i => i.DiscordGuild.DiscordId == Context.Guild.Id);
            subscriptions = subscriptions.OrderBy(i => i.User.DisplayName);

            if (!subscriptions.Any())
            {
                await FollowupAsync("There are no subscriptions for this server!", ephemeral: true);
                return;
            }

            var subscription = subscriptions.First();

            var subscriptionEmbed = MonitorUtils.GetSubscriptionEmbed(currentSpot: 0, subscription: subscription, subscriptionCount: subscriptions.Count());
            var messageComponents = MonitorUtils.GetSubscriptionComponents(subscription: subscription, context: Context, previousSpot: -1, nextSpot: 1);

            await FollowupAsync(text: $"Streams being monitored for this server", ephemeral: true, embed: subscriptionEmbed, components: messageComponents);
        }

        #endregion list command
    }

    public class MonitorListModule : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly ILogger<MonitorListModule> _logger;
        private readonly IUnitOfWork _work;

        public MonitorListModule(ILogger<MonitorListModule> logger, IUnitOfWorkFactory factory)
        {
            _logger = logger;
            _work = factory.Create();
        }

        #region monitor.list

        [RequireBotManager]
        [ComponentInteraction(customId: "monitor.list:*")]
        public async Task MonitorListAsync(int currentSpot)
        {
            if (Context.Interaction is SocketMessageComponent component)
            {
                var subscriptions = await _work.SubscriptionRepository.FindAsync(i => i.DiscordGuild.DiscordId == Context.Guild.Id);
                subscriptions = subscriptions.OrderBy(i => i.User.DisplayName);

                if (!subscriptions.Any())
                {
                    await component.UpdateAsync(x =>
                    {
                        x.Content = "There are no subscriptions for this server!";
                        x.Embed = null;
                        x.Components = null;
                    });
                    return;
                }

                if (currentSpot > subscriptions.Count() - 1)
                    currentSpot -= subscriptions.Count();
                if (currentSpot < 0)
                    currentSpot = subscriptions.Count() - 1;

                var subscription = subscriptions.ToList()[currentSpot];

                int previousSpot = currentSpot - 1;
                int nextSpot = currentSpot + 1;

                if (previousSpot < 0)
                    previousSpot = subscriptions.Count() - 1;

                if (nextSpot > subscriptions.Count() - 1)
                    nextSpot = 0;

                if (subscriptions.Count() <= 2)
                {
                    if (currentSpot == 0)
                    {
                        previousSpot = -1;
                        nextSpot = 1;
                    }

                    if (currentSpot == 1)
                    {
                        previousSpot = 0;
                        nextSpot = 2;
                    }
                }

                var subscriptionEmbed = MonitorUtils.GetSubscriptionEmbed(currentSpot: currentSpot, subscription: subscription, subscriptionCount: subscriptions.Count());
                var messageComponents = MonitorUtils.GetSubscriptionComponents(subscription: subscription, context: Context, previousSpot: previousSpot, nextSpot: nextSpot);

                await component.UpdateAsync(x =>
                {
                    x.Content = $"Streams being monitored for this server";
                    x.Embed = subscriptionEmbed;
                    x.Components = messageComponents;
                });
            }
        }

        #endregion monitor.list

        #region monitor.delete

        [RequireBotManager]
        [ComponentInteraction(customId: "monitor.delete:*")]
        public async Task MonitorDeleteAsync(long subscriptionId)
        {
            await DeferAsync(ephemeral: true);
            var subscription = await _work.SubscriptionRepository.GetAsync(subscriptionId);

            if (subscription == null)
                throw new ArgumentException("This subscription does not exist, maybe it was already deleted?");

            var displayName = Format.Bold(subscription.User.DisplayName);

            var componentBuilder = new ComponentBuilder()
                .WithButton(label: "Confirm", customId: $"monitor.delete.confirm:{subscriptionId},{true}", style: ButtonStyle.Success)
                .WithButton(label: "Cancel", customId: $"monitor.delete.confirm:{subscriptionId},{false}", style: ButtonStyle.Danger);

            await FollowupAsync(text: $"Are you sure you wish to delete the subscription for {displayName}?", ephemeral: true, components: componentBuilder.Build());
        }

        #endregion monitor.delete

        #region monitor.delete.confirm

        [RequireBotManager]
        [ComponentInteraction(customId: "monitor.delete.confirm:*,*")]
        public async Task MonitorDeleteConfirmAsync(long subscriptionId, bool delete)
        {
            if (Context.Interaction is SocketMessageComponent component)
            {
                var subscription = await _work.SubscriptionRepository.GetAsync(subscriptionId);
                var displayName = Format.Bold(subscription.User.DisplayName);
                var resultMessage = "Unkown error occurred";

                if (delete)
                {
                    try
                    {
                        await _work.SubscriptionRepository.RemoveAsync(subscription.Id);

                        _logger.LogInformation(
                            message: "Stream Subscription deleted for {ServiceType} {StreamUsername} ({StreamUserId}) by {Username} ({UserId}) in {GuildName} ({GuildId}))",
                            subscription.User.ServiceType,
                            subscription.User.Username,
                            subscription.User.SourceID,
                            Format.UsernameAndDiscriminator(user: Context.User, doBidirectional: true),
                            Context.User.Id,
                            Context.Guild.Name,
                            Context.Guild.Id
                        );

                        resultMessage = $"Monitor for {displayName} has been deleted";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            exception: ex,
                            message: "Stream Subscription could not be deleted for {ServiceType} {StreamUsername} ({StreamUserId}) by {Username} ({UserId}) in {GuildName} ({GuildId}))",
                            subscription.User.ServiceType,
                            subscription.User.Username,
                            subscription.User.SourceID,
                            Format.UsernameAndDiscriminator(user: Context.User, doBidirectional: true),
                            Context.User.Id,
                            Context.Guild.Name,
                            Context.Guild.Id
                        );

                        resultMessage = $"Unable to remove subscription for {displayName}";
                    }
                }
                else
                {
                    resultMessage = $"Cancelled deleting monitor for {displayName}";
                }

                await component.UpdateAsync(x =>
                {
                    x.Content = resultMessage;
                    x.Components = null;
                });
            }
        }

        #endregion monitor.delete.confirm

        #region monitor.edit.roles

        [ComponentInteraction(customId: "monitor.edit.roles:*")]
        public async Task UpdateRolesAsync(long subscriptionId, string[] roleIds)
        {
            await DeferAsync(ephemeral: true);
            var subscription = await _work.SubscriptionRepository.GetAsync(subscriptionId);

            if (roleIds.Length == 0)
                subscription.DiscordRole = null;
            else
            {
                foreach (var roleId in roleIds)
                {
                    var discordRole = await _work.RoleRepository.SingleOrDefaultAsync(i => i.DiscordId.ToString() == roleId);
                    subscription.DiscordRole = discordRole;
                }
            }

            await _work.SubscriptionRepository.UpdateAsync(subscription);

            await FollowupAsync(text: $"Monitor for {Format.Bold(subscription.User.DisplayName)} has been updated!", ephemeral: true);
        }

        #endregion monitor.edit.roles
    }

    internal static class MonitorUtils
    {
        internal static SelectMenuBuilder GetRoleMentionSelectMenu(StreamSubscription subscription, SocketGuild guild)
        {
            var selectMenu = new SelectMenuBuilder()
            {
                CustomId = $"monitor.edit.roles:{subscription.Id}",
                Placeholder = "Role to mention",
                MinValues = 0,
                MaxValues = 1,
            };

            var selectedIds = new[] { subscription.DiscordRole?.DiscordId };
            foreach (var role in guild.Roles)
            {
                if (selectMenu.Options.Count >= 25)
                    break;
                var isDefault = selectedIds.Contains(role.Id);
                selectMenu.AddOption(label: role.Name, value: role.Id.ToString(), isDefault: isDefault);
            }
            return selectMenu;
        }

        internal static MessageComponent GetSubscriptionComponents(StreamSubscription subscription, ShardedInteractionContext context, int previousSpot = 0, int nextSpot = 1) =>
            new ComponentBuilder()
            .WithSelectMenu(menu: GetRoleMentionSelectMenu(subscription: subscription, guild: context.Guild))
            .WithButton(label: "Back", customId: $"monitor.list:{previousSpot}", style: ButtonStyle.Primary, emote: new Emoji("\u25C0"))
            .WithButton(label: "Next", customId: $"monitor.list:{nextSpot}", style: ButtonStyle.Primary, emote: new Emoji("\u25B6"))
            .WithButton(label: "Delete", customId: $"monitor.delete:{subscription.Id}", style: ButtonStyle.Danger, emote: new Emoji("\uD83D\uDDD1"))
            .Build();

        internal static Embed GetSubscriptionEmbed(StreamSubscription subscription, int subscriptionCount, int currentSpot = 0) =>
            new EmbedBuilder()
            .WithColor(subscription.User.ServiceType.GetAlertColor())
            .WithThumbnailUrl(subscription.User.AvatarURL)
            .WithAuthor(name: subscription.User.DisplayName, iconUrl: subscription.User.AvatarURL, url: subscription.User.ProfileURL)

            .AddField(name: "Profile", value: subscription.User.ProfileURL, inline: true)
            .AddField(name: "Channel", value: MentionUtils.MentionChannel(subscription.DiscordChannel.DiscordId), inline: true)
            .AddField(name: "Role", value: "See the dropdown below", inline: false)
            .AddField(name: "Message", value: subscription.Message, inline: false)

            .WithFooter(text: $"Page {currentSpot + 1}/{subscriptionCount}")
            .Build();
    }
}