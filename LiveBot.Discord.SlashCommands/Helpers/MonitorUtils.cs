﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;

namespace LiveBot.Discord.SlashCommands.Helpers
{
    internal static class MonitorUtils
    {
        internal static SelectMenuBuilder GetRoleMentionSelectMenu(StreamSubscription subscription, SocketGuild guild)
        {
            var selectMenu = new SelectMenuBuilder()
            {
                CustomId = $"monitor.edit.roles:{subscription.Id}",
                Placeholder = "Role to mention",
                MinValues = 0,
                MaxValues = 5,
            };

            var selectedIds = subscription.RolesToMention.Select(i => i.DiscordRoleId).Distinct();
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
            .AddField(name: "Message", value: subscription.Message, inline: false)
            .AddField(name: "Roles", value: String.Join(", ", subscription.RolesToMention.Select(i => MentionUtils.MentionRole(i.DiscordRoleId))), inline: false)

            .WithFooter(text: $"Page {currentSpot + 1}/{subscriptionCount}")
            .Build();
    }
}