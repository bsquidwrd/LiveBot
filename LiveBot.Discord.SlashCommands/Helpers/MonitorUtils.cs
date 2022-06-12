using Discord;
using Discord.WebSocket;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;

namespace LiveBot.Discord.SlashCommands.Helpers
{
    internal static class MonitorUtils
    {
        internal static SelectMenuBuilder GetRoleMentionSelectMenu(StreamSubscription subscription, SocketGuild guild)
        {
            var maxRoleSelections = 5;
            if (guild.Roles.Count < 5)
                maxRoleSelections = guild.Roles.Count;
            var selectMenu = new SelectMenuBuilder()
            {
                CustomId = $"monitor.edit.roles:{subscription.Id}",
                Placeholder = "Role to mention",
                MinValues = 0,
                MaxValues = maxRoleSelections,
            };

            IEnumerable<ulong> selectedIds = new List<ulong>() { };
            if (subscription.RolesToMention != null)
                selectedIds = subscription.RolesToMention.Select(i => i.DiscordRoleId).Distinct();
            foreach (var role in guild.Roles)
            {
                if (selectMenu.Options.Count >= 25)
                    break;
                var isDefault = selectedIds.Contains(role.Id);
                selectMenu.AddOption(label: role.Name, value: role.Id.ToString(), isDefault: isDefault);
            }
            return selectMenu;
        }

        internal static MessageComponent GetSubscriptionComponents(StreamSubscription subscription, int previousSpot = 0, int nextSpot = 1) =>
            new ComponentBuilder()
            .WithButton(label: "Back", customId: $"monitor.list:{previousSpot}", style: ButtonStyle.Primary, emote: new Emoji("\u25C0"))
            .WithButton(label: "Next", customId: $"monitor.list:{nextSpot}", style: ButtonStyle.Primary, emote: new Emoji("\u25B6"))
            .WithButton(label: "Delete", customId: $"monitor.delete:{subscription.Id}", style: ButtonStyle.Danger, emote: new Emoji("\uD83D\uDDD1"))
            .Build();

        internal static Embed GetSubscriptionEmbed(SocketGuild guild, StreamSubscription subscription, int subscriptionCount, int currentSpot = 0)
        {
            var roles = new List<SocketRole>();
            if (subscription.RolesToMention.Any())
                roles = subscription.RolesToMention.Select(i => guild.GetRole(i.DiscordRoleId)).ToList();

            var roleStrings = new List<string>();
            foreach (var role in roles.OrderBy(i => i.Name))
            {
                if (role.Name.Equals("@everyone", StringComparison.CurrentCulture))
                    roleStrings.Add("@everyone");
                else if (role.Name.Equals("@here", StringComparison.CurrentCulture))
                    roleStrings.Add("@here");
                else
                    roleStrings.Add(role.Mention);
            }

            return new EmbedBuilder()
                .WithColor(subscription.User.ServiceType.GetAlertColor())
                .WithThumbnailUrl(subscription.User.AvatarURL)
                .WithAuthor(name: subscription.User.DisplayName, iconUrl: subscription.User.AvatarURL, url: subscription.User.ProfileURL)

                .AddField(name: "Profile", value: subscription.User.ProfileURL, inline: true)
                .AddField(name: "Channel", value: MentionUtils.MentionChannel(subscription.DiscordChannel.DiscordId), inline: true)
                .AddField(name: "Message", value: subscription.Message, inline: false)
                .AddField(name: "Roles", value: !subscription.RolesToMention.Any() ? "none" : String.Join(", ", roleStrings), inline: false)

                .WithFooter(text: $"Page {currentSpot + 1}/{subscriptionCount}")
                .Build();
        }

        internal static async Task<bool> ConsolidateRoleMentions(IUnitOfWork work, StreamSubscription subscription, params IRole?[] roles)
        {
            var RolesUpdated = false;
            var roleIds = new List<ulong>();
            foreach (var role in roles)
                if (role?.Id != null)
                    roleIds.Add(role.Id);

            if (roleIds.Count > 0)
            {
                var currentRolesToMention = await work.RoleToMentionRepository.FindAsync(i => i.StreamSubscription == subscription);
                if (currentRolesToMention.Any())
                {
                    var rolesToDelete = currentRolesToMention.Where(i => !roleIds.Contains(i.DiscordRoleId)).ToList();
                    rolesToDelete.ForEach(async i => await work.RoleToMentionRepository.RemoveAsync(i.Id));
                    RolesUpdated = true;
                }
                foreach (var roleId in roleIds)
                {
                    var currentRoleToMention = await work.RoleToMentionRepository.SingleOrDefaultAsync(i => i.StreamSubscription == subscription && i.DiscordRoleId == roleId);
                    if (currentRoleToMention == null)
                    {
                        await work.RoleToMentionRepository.AddAsync(new RoleToMention
                        {
                            StreamSubscription = subscription,
                            DiscordRoleId = roleId,
                        });
                        RolesUpdated = true;
                    }
                }
            }
            return RolesUpdated;
        }
    }
}