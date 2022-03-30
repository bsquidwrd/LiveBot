﻿using Discord.Rest;
using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using LiveBot.Discord.SlashCommands.Contracts.Discord;
using MassTransit;

namespace LiveBot.Discord.SlashCommands.Consumers.Discord
{
    public class DiscordGuildAvailableConsumer : IConsumer<IDiscordGuildAvailable>
    {
        private readonly ILogger<DiscordGuildAvailableConsumer> _logger;
        private readonly IUnitOfWork _work;
        private readonly DiscordRestClient _client;
        private readonly IBusControl _bus;

        public DiscordGuildAvailableConsumer(ILogger<DiscordGuildAvailableConsumer> logger, IUnitOfWorkFactory factory, DiscordRestClient client, IBusControl bus)
        {
            _logger = logger;
            _work = factory.Create();
            _client = client;
            _bus = bus;
        }

        public async Task Consume(ConsumeContext<IDiscordGuildAvailable> context)
        {
            var message = context.Message;
            var guild = await _client.GetGuildAsync(message.GuildId);

            if (guild == null)
                return;

            var existingDiscordGuild = await _work.GuildRepository.SingleOrDefaultAsync(d => d.DiscordId == message.GuildId);

            var newDiscordGuild = new DiscordGuild()
            {
                DiscordId = message.GuildId,
                Name = message.GuildName,
                IconUrl = guild.IconUrl,
                IsInBeta = existingDiscordGuild?.IsInBeta ?? false
            };

            await _work.GuildRepository.AddOrUpdateAsync(newDiscordGuild, (d => d.DiscordId == message.GuildId));
            var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(d => d.DiscordId == message.GuildId);

            #region Handle Channels

            var dbChannels = await _work.ChannelRepository.FindAsync(i => i.DiscordGuild == discordGuild);

            var guildTextChannels = await guild.GetTextChannelsAsync();
            foreach (var channel in guildTextChannels)
            {
                var existingChannels = dbChannels.Where(i => i.DiscordId == channel.Id && i.Name == channel.Name);
                if (existingChannels.Any())
                    continue;
                await _bus.Publish(new DiscordChannelUpdate
                {
                    GuildId = guild.Id,
                    ChannelId = channel.Id,
                    ChannelName = channel.Name,
                });
            }

            var channelIDs = guildTextChannels.Select(i => i.Id).Distinct().ToList();
            if (dbChannels.Any())
            {
                foreach (var channelId in dbChannels.Select(i => i.DiscordId).Distinct().Except(channelIDs))
                {
                    await _bus.Publish(new DiscordChannelDelete
                    {
                        GuildId = guild.Id,
                        ChannelId = channelId,
                    });
                }
            }

            #endregion Handle Channels

            #region Handle Roles

            var dbRoles = await _work.RoleRepository.FindAsync(i => i.DiscordGuild == discordGuild);

            foreach (var role in guild.Roles)
            {
                var existingRoles = dbRoles.Where(i => i.DiscordId == role.Id && i.Name == role.Name);
                if (existingRoles.Any())
                    continue;
                await _bus.Publish(new DiscordRoleUpdate
                {
                    GuildId = guild.Id,
                    RoleId = role.Id,
                    RoleName = role.Name,
                });
            }

            var roleIDs = guild.Roles.Select(i => i.Id).Distinct().ToList();
            if (dbRoles.Any())
            {
                foreach (var roleId in dbRoles.Select(i => i.DiscordId).Distinct().Except(roleIDs))
                {
                    ;
                    await _bus.Publish(new DiscordRoleDelete
                    {
                        GuildId = guild.Id,
                        RoleId = roleId,
                    });
                }
            }

            #endregion Handle Roles
        }
    }
}