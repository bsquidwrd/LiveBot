﻿using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using MassTransit;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers.Discord
{
    public class DiscordChannelUpdateConsumer : IConsumer<IDiscordChannelUpdate>
    {
        private readonly IUnitOfWork _work;

        public DiscordChannelUpdateConsumer(IUnitOfWorkFactory factory)
        {
            _work = factory.Create();
        }

        public async Task Consume(ConsumeContext<IDiscordChannelUpdate> context)
        {
            var message = context.Message;
            var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == message.GuildId);
            var discordChannel = new DiscordChannel
            {
                DiscordGuild = discordGuild,
                DiscordId = message.ChannelId,
                Name = message.ChannelName
            };
            await _work.ChannelRepository.AddOrUpdateAsync(discordChannel, i => i.DiscordGuild == discordGuild && i.DiscordId == message.ChannelId);
        }
    }
}