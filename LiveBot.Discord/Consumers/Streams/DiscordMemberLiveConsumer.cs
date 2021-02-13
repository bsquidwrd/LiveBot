using Discord.WebSocket;
using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Discord;
using LiveBot.Core.Repository.Models.Streams;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers.Streams
{
    public class DiscordMemberLiveConsumer : IConsumer<IDiscordMemberLive>
    {
        private readonly DiscordShardedClient _client;
        private readonly IUnitOfWork _work;
        private readonly IEnumerable<ILiveBotMonitor> _monitors;

        public DiscordMemberLiveConsumer(DiscordShardedClient client, IUnitOfWorkFactory factory, IEnumerable<ILiveBotMonitor> monitors)
        {
            _client = client;
            _work = factory.Create();
            _monitors = monitors;
        }

        public async Task Consume(ConsumeContext<IDiscordMemberLive> context)
        {
            var monitor = _monitors.Where(i => i.ServiceType == context.Message.ServiceType).FirstOrDefault();
            if (monitor == null) return;

            DiscordGuild discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == context.Message.DiscordGuildId && i.IsInBeta == true);

            if (discordGuild == null) return;

            // If the Guild ID is not whitelisted, don't do anything This is for Beta testing
            bool isInBeta = discordGuild?.IsInBeta ?? false;
            if (!isInBeta) return;

            DiscordGuildConfig guildConfig = await _work.GuildConfigRepository.SingleOrDefaultAsync(i => i.DiscordGuild.DiscordId == context.Message.DiscordGuildId);

            // If they don't have any of the proper settings set, ignore
            if (guildConfig == null) return;
            if (guildConfig.MonitorRole == null || guildConfig.DiscordChannel == null || guildConfig.Message == null)
                return;

            ILiveBotUser user = await monitor.GetUser(profileURL: context.Message.Url);
            StreamUser streamUser = new StreamUser()
            {
                ServiceType = user.ServiceType,
                SourceID = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                AvatarURL = user.AvatarURL,
                ProfileURL = user.ProfileURL
            };
            await _work.UserRepository.AddOrUpdateAsync(streamUser, (i => i.ServiceType == user.ServiceType && i.SourceID == user.Id));
            streamUser = await _work.UserRepository.SingleOrDefaultAsync(i => i.ServiceType == user.ServiceType && i.SourceID == user.Id);

            Expression<Func<StreamSubscription, bool>> streamSubscriptionPredicate = (i =>
                i.User == streamUser &&
                i.DiscordGuild == discordGuild
            );

            StreamSubscription existingSubscription = await _work.SubscriptionRepository.SingleOrDefaultAsync(streamSubscriptionPredicate);

            var guild = _client.GetGuild(discordGuild.DiscordId);
            if (guild == null) return;
            var guildMember = guild.GetUser(context.Message.DiscordUserId);

            var userHasMonitorRole = guildMember.Roles.Select(i => i.Id).Distinct().Contains(guildConfig.MonitorRole.DiscordId);

            // If there's an existing subscription, check that they still have the role
            if (existingSubscription != null)
            {
                // If it's not from a role, just return and stop processing
                if (!existingSubscription.IsFromRole)
                    return;

                // If the Discord User Id is null, populate it
                if (existingSubscription.DiscordUserId == null)
                {
                    existingSubscription.DiscordUserId = context.Message.DiscordUserId;
                    await _work.SubscriptionRepository.UpdateAsync(existingSubscription);
                }

                // If the user does not have the role, remove their subscription
                if (!userHasMonitorRole)
                    await _work.SubscriptionRepository.RemoveAsync(existingSubscription.Id);
            }

            if (!userHasMonitorRole)
                return;

            StreamSubscription newSubscription = new StreamSubscription()
            {
                User = streamUser,
                DiscordGuild = discordGuild,
                DiscordChannel = guildConfig.DiscordChannel,
                DiscordRole = guildConfig.DiscordRole,
                Message = guildConfig.Message,
                IsFromRole = true,
                DiscordUserId = context.Message.DiscordUserId
            };

            await _work.SubscriptionRepository.AddOrUpdateAsync(newSubscription, streamSubscriptionPredicate);

            // Check that it was created
            StreamSubscription streamSubscription = await _work.SubscriptionRepository.SingleOrDefaultAsync(streamSubscriptionPredicate);

            // If it was created, add them to the Monitor
            if (streamSubscription != null)
                monitor.AddChannel(user);
        }
    }
}