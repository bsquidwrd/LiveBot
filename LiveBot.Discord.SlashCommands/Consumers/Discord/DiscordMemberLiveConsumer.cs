using Discord.WebSocket;
using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using MassTransit;
using System.Linq.Expressions;

namespace LiveBot.Discord.SlashCommands.Consumers.Discord
{
    public class DiscordMemberLiveConsumer : IConsumer<IDiscordMemberLive>
    {
        private readonly DiscordShardedClient _client;
        private readonly IUnitOfWork _work;
        private readonly IEnumerable<ILiveBotMonitor> _monitors;
        private readonly ILogger<DiscordMemberLiveConsumer> _logger;

        public DiscordMemberLiveConsumer(DiscordShardedClient client, IUnitOfWorkFactory factory, IEnumerable<ILiveBotMonitor> monitors, ILogger<DiscordMemberLiveConsumer> logger)
        {
            _client = client;
            _work = factory.Create();
            _monitors = monitors;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IDiscordMemberLive> context)
        {
            var monitor = _monitors.Where(i => i.IsValid(context.Message.Url)).FirstOrDefault();
            if (monitor == null) return;

            var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == context.Message.DiscordGuildId && i.IsInBeta == true);

            if (discordGuild == null) return;

            // If the Guild ID is not whitelisted, don't do anything This is for Beta testing
            bool isInBeta = discordGuild?.IsInBeta ?? false;
            if (!isInBeta) return;

            var guildConfig = await _work.GuildConfigRepository.SingleOrDefaultAsync(i => i.DiscordGuild.DiscordId == context.Message.DiscordGuildId);

            // If they don't have any of the proper settings set, ignore
            if (guildConfig == null) return;
            if (guildConfig.MonitorRole == null || guildConfig.DiscordChannel == null || guildConfig.Message == null)
                return;

            var user = await monitor.GetUser(profileURL: context.Message.Url);
            var streamUser = new StreamUser()
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

            var existingSubscription = await _work.SubscriptionRepository.SingleOrDefaultAsync(streamSubscriptionPredicate);

            var guild = _client.GetGuild(context.Message.DiscordGuildId);
            if (guild == null) return;
            var guildMember = guild.GetUser(context.Message.DiscordUserId);

            var userHasMonitorRole = guildMember.Roles.Select(i => i.Id).Distinct().Contains(guildConfig.MonitorRole.DiscordId);

            // If there's an existing subscription, check that they still have the role
            if (existingSubscription != null)
            {
                // If it's not from a role, just return and stop processing
                if (!existingSubscription.IsFromRole)
                    return;
                // If the user does not have the role, remove their subscription
                if (!userHasMonitorRole)
                    await _work.SubscriptionRepository.RemoveAsync(existingSubscription.Id);
            }

            if (!userHasMonitorRole)
                return;

            var newSubscription = new StreamSubscription()
            {
                User = streamUser,
                DiscordGuild = discordGuild,
                DiscordChannel = guildConfig.DiscordChannel,
                DiscordRole = guildConfig.DiscordRole,
                Message = guildConfig.Message,
                IsFromRole = true
            };

            await _work.SubscriptionRepository.AddOrUpdateAsync(newSubscription, streamSubscriptionPredicate);

            // Check that it was created
            var streamSubscription = await _work.SubscriptionRepository.SingleOrDefaultAsync(streamSubscriptionPredicate);
            monitor.AddChannel(user);
        }
    }
}