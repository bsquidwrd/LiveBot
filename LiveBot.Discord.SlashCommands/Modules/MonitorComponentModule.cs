using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Discord.SlashCommands.Attributes;
using LiveBot.Discord.SlashCommands.Helpers;

namespace LiveBot.Discord.SlashCommands.Modules
{
    public class MonitorComponentModule : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly ILogger<MonitorComponentModule> _logger;
        private readonly IUnitOfWork _work;

        public MonitorComponentModule(ILogger<MonitorComponentModule> logger, IUnitOfWorkFactory factory)
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

        [RequireBotManager]
        [ComponentInteraction(customId: "monitor.edit.roles:*")]
        public async Task UpdateRolesAsync(long subscriptionId, string[] roleIds)
        {
            if (Context.Interaction is SocketMessageComponent component)
            {
                var subscription = await _work.SubscriptionRepository.GetAsync(subscriptionId);
                var roleMentions = subscription.RolesToMention;

                foreach (var roleMention in roleMentions.Where(i => !roleIds.Contains(i.DiscordRoleId.ToString())))
                    await _work.RoleToMentionRepository.RemoveAsync(roleMention.Id);

                foreach (var roleId in roleIds.Where(i => !roleMentions.Select(r => r.DiscordRoleId.ToString()).Contains(i)))
                    await _work.RoleToMentionRepository.AddAsync(new RoleToMention()
                    {
                        StreamSubscription = subscription,
                        DiscordRoleId = ulong.Parse(roleId),
                    });

                if (subscription.RolesToMention.Any() && !subscription.Message.Contains("{role}", StringComparison.InvariantCultureIgnoreCase))
                    subscription.Message = String.Concat("{role} ", subscription.Message).Trim();
                await _work.SubscriptionRepository.UpdateAsync(subscription);

                await component.UpdateAsync(x =>
                {
                    x.Content = $"Roles to mention for {Format.Bold(subscription.User.DisplayName)} has been updated!";
                    x.Components = null;
                    x.Embed = null;
                    x.Embeds = null;
                });
            }
        }

        #endregion monitor.edit.roles
    }
}