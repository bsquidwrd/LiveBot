using Discord;
using Discord.WebSocket;
using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Static;
using MassTransit;
using System.Globalization;

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
            var memberLive = context.Message;

            var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == memberLive.DiscordGuildId && i.IsInBeta == true);

            if (discordGuild == null) return;

            // If the Guild ID is not whitelisted, don't do anything This is for Beta testing
            bool isInBeta = discordGuild?.IsInBeta ?? false;
            if (!isInBeta) return;

            var guildConfig = await _work.GuildConfigRepository.SingleOrDefaultAsync(i => i.DiscordGuild.DiscordId == context.Message.DiscordGuildId);

            // If they don't have any of the proper settings set, ignore
            if (guildConfig == null) return;
            if (guildConfig.MonitorRoleDiscordId == null || guildConfig.DiscordChannel == null || guildConfig.Message == null)
                return;

            // Get the guild
            var guild = _client.GetGuild(memberLive.DiscordGuildId);
            if (guild == null) return;

            // Get the guild user
            var user = guild.GetUser(memberLive.DiscordUserId);
            if (user == null) return;

            // If the user doesn't have the monitor role, return
            if (!user.Roles.Any(i => i.Id == guildConfig.MonitorRoleDiscordId)) return;

            // Get the channel
            var channel = guild.GetTextChannel(guildConfig.DiscordChannel.DiscordId);
            if (channel == null) return;

            // Default to none
            var alertColor = ServiceEnum.None.GetAlertColor();

            // Attempt to get the color specific to the streaming service
            var monitor = _monitors.Where(i => i.IsValid(context.Message.Url)).FirstOrDefault();
            if (monitor != null)
                monitor.ServiceType.GetAlertColor();

            var embed = new EmbedBuilder()
                .WithColor(color: alertColor)
                .WithDescription(description: Format.Sanitize(memberLive.GameDetails))
                .WithAuthor(user: user)
                .WithFooter(text: "Stream start time")
                .WithCurrentTimestamp()
                .WithUrl(url: memberLive.Url)
                .WithThumbnailUrl(thumbnailUrl: user.GetGuildAvatarUrl() ?? user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())

                .AddField(name: "Game", value: memberLive.GameName, inline: true)
                .AddField(name: "Stream", value: memberLive.Url, inline: true)

                .Build();

            var roleToMention = "";
            if (guildConfig.MentionRoleDiscordId != null)
                roleToMention = MentionUtils.MentionRole((ulong)guildConfig.MentionRoleDiscordId);

            var message = guildConfig.Message
                .Replace("{Name}", Format.Sanitize(user.DisplayName), ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{Username}", Format.Sanitize(user.DisplayName), ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{Game}", Format.Sanitize(memberLive.GameName), ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{Title}", Format.Sanitize(memberLive.GameDetails), ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{URL}", Format.EscapeUrl(memberLive.Url) ?? "", ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{Role}", roleToMention, ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Trim();

            var discordMessage = await channel.SendMessageAsync(text: message, embed: embed);
        }
    }
}