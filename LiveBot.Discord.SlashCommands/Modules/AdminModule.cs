using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Discord;
using LiveBot.Core.Repository.Static;
using System.Globalization;

namespace LiveBot.Discord.SlashCommands.Modules
{
    [RequireOwner]
    [DontAutoRegister]
    [Group(name: "admin", description: "Administrative functions for the Bot Owner")]
    public class AdminModule : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly ILogger<AdminModule> logger;
        private readonly IConfiguration configuration;
        private readonly InteractionService commands;
        private readonly IUnitOfWork work;
        private readonly IEnumerable<ILiveBotMonitor> monitors;

        public AdminModule(ILogger<AdminModule> logger, IConfiguration configuration, InteractionService commands, IUnitOfWorkFactory factory, IEnumerable<ILiveBotMonitor> monitors)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.commands = commands;
            this.work = factory.Create();
            this.monitors = monitors;
        }

        #region Ping command

        [SlashCommand(name: "ping", description: "Ping the bot")]
        public async Task PingAsync()
        {
            var timeDifference = DateTimeOffset.UtcNow - Context.Interaction.CreatedAt.ToUniversalTime();
            await FollowupAsync(text: $"Took {timeDifference:hh\\:mm\\:ss\\.fff} to respond", ephemeral: true);
        }

        #endregion Ping command

        #region Divide command

        [SlashCommand(name: "divide", description: "Divide two numbers")]
        public async Task DivideAsync(int number1, int number2)
        {
            await FollowupAsync(text: $"Result: {number1 / number2}", ephemeral: true);
        }

        #endregion Divide command

        #region Register command

        [SlashCommand(name: "register", description: "Force a re-registration of the bot commands")]
        public async Task RegisterCommandsAsync()
        {
            var startTime = DateTime.UtcNow;
            var IsDebug = Convert.ToBoolean(configuration.GetValue<string>("IsDebug") ?? "false");
            var testGuildId = configuration.GetValue<ulong>("testguild");

            if (IsDebug)
                await commands.RegisterCommandsToGuildAsync(guildId: testGuildId, deleteMissing: true);
            else
                await commands.RegisterCommandsGloballyAsync(deleteMissing: true);

            var adminGuild = await commands.RestClient.GetGuildAsync(testGuildId);
            await commands.AddModulesToGuildAsync(guild: adminGuild, deleteMissing: false, modules: commands.GetModuleInfo<AdminModule>());

            var endTime = DateTime.UtcNow;
            var timeDifference = endTime - startTime;

            await FollowupAsync(text: $"Finished registering commands. Took {timeDifference:hh\\:mm\\:ss\\.fff}", ephemeral: true);
        }

        #endregion Register command

        #region Beta command

        [SlashCommand(name: "beta", description: "Change beta status for a given Guild Id")]
        public async Task BetaSettingAsync(string guildId, bool enabled)
        {
            _ = ulong.TryParse(guildId, out var parsedGuildId);
            var guild = Context.Client.GetGuild(parsedGuildId);
            if (guild == null)
                throw new Exception($"Guild not found with Id {parsedGuildId}");

            var discordGuild = await work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == guild.Id);
            if (discordGuild == null)
            {
                var newDiscordGuild = new DiscordGuild
                {
                    DiscordId = guild.Id,
                    Name = guild.Name,
                    IconUrl = guild.IconUrl,
                };
                await work.GuildRepository.AddAsync(newDiscordGuild);
                discordGuild = await work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == guild.Id);
            }

            discordGuild.IsInBeta = enabled;
            await work.GuildRepository.UpdateAsync(discordGuild);
            discordGuild = await work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == guild.Id);

            logger.LogInformation(message: "Set Beta status of {GuildName} ({GuildId}) to {BetaStatus}", discordGuild.Name, discordGuild.DiscordId.ToString(), discordGuild.IsInBeta);

            await FollowupAsync(text: $"I have updated the beta status for {Format.Code(discordGuild.Name)} to {Format.Code(discordGuild.IsInBeta.ToString())}", ephemeral: true);
        }

        #endregion Beta command

        #region Guild Info command

        /// <summary>
        /// Given an Id, get some basic information about the Guild that may help
        /// when there seems to be an issue with the bot
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [SlashCommand(name: "guildinfo", description: "Get misc information of a guild (used for troubleshooting)")]
        public async Task AdminGuildInfoCommandAsync(
            [Summary(name: "id", description: "The Guild Id to get information for")] string? id = null
        )
        {
            if (id == null)
                id = Context.Guild.Id.ToString();
            if (!ulong.TryParse(id, out var guildId))
                throw new Exception("Could not parse Guild Id");

            var guild = Context.Client.GetGuild(guildId);
            if (guild == null)
                throw new Exception($"Unable to find the Guild with Id {guildId}");

            var discordGuild = await work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == guild.Id);
            var notifications = await work.NotificationRepository.FindAsync(i => i.DiscordGuild_DiscordId == guild.Id && i.Success == true && i.DiscordMessage_DiscordId != null);
            var latestNotification = notifications.OrderByDescending(i => i.TimeStamp).FirstOrDefault();

            var embedBuilder = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithCurrentTimestamp()
                .WithAuthor(guild.Owner)
                .WithThumbnailUrl(guild.IconUrl)
                .WithDescription($"");

            embedBuilder
                .AddField(name: "Creation Date", value: $"{guild.CreatedAt.UtcDateTime.ToString("u", CultureInfo.GetCultureInfo("en-US"))}", inline: true)
                .AddField(name: "Subscription Count", value: $"{discordGuild.StreamSubscriptions.Count}", inline: true)
                .AddField(name: "Notification Count", value: $"{notifications.Count()}", inline: true)
                .AddField(name: "Latest Notification", value: $"{latestNotification?.ServiceType} {latestNotification?.User_Username ?? "none"}", inline: true);

            foreach (var serviceType in notifications.Select(i => i.ServiceType).Distinct())
                embedBuilder.AddField(name: $"{serviceType} Subscriptions", value: notifications.Count(i => i.ServiceType == serviceType), inline: true);

            await FollowupAsync(text: $"Guild information for {Format.Bold(guild.Name)}", embed: embedBuilder.Build(), ephemeral: true);
        }

        #endregion Guild Info command

        #region User Info Command

        /// <summary>
        /// Get information about a user, and their mutual guilds with the bot
        /// </summary>
        /// <param name="id"></param>
        /// <param name="gid"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [SlashCommand(name: "userinfo", description: "Get misc information about a user (used for troubleshooting)")]
        public async Task AdminUserInfoCommandAsync(
            [Summary(name: "id", description: "The User Id to get information for")] string id,
            [Summary(name: "guildid", description: "The Guild Id to check too")] string? gid = null
        )
        {
            if (!ulong.TryParse(id, out var userId))
                throw new Exception("Could not parse User Id");

            var user = Context.Client.GetUser(userId);

            var userEmbedBuilder = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithAuthor(user)
                .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                .WithFooter(user.Id.ToString());

            var mutualGuilds = new List<SocketGuild>();
            foreach (DiscordSocketClient shard in Context.Client.Shards)
            {
                foreach (SocketGuild guild in shard.Guilds)
                {
                    if (guild.GetUser(user.Id) != null)
                        if (!mutualGuilds.Any(i => i.Id == guild.Id))
                            mutualGuilds.Add(guild);
                }
            }

            userEmbedBuilder
                .AddField(name: "Created Date", value: $"{TimestampTag.FromDateTime(user.CreatedAt.UtcDateTime, TimestampTagStyles.Relative)}", inline: true)
                .AddField(name: "Mutual Servers", value: $"{mutualGuilds.Count}", inline: true);

            await FollowupAsync(text: $"User information for {Format.Bold(Format.UsernameAndDiscriminator(user: user, doBidirectional: true))}", embed: userEmbedBuilder.Build(), ephemeral: true);

            foreach (var guildChunk in mutualGuilds.Chunk(10))
            {
                var guildEmbeds = new List<Embed>();

                foreach (var guild in guildChunk)
                {
                    if (gid != null)
                        if (gid != guild.Id.ToString())
                            continue;

                    var discordGuild = await work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == guild.Id);
                    var notifications = await work.NotificationRepository.FindAsync(i => i.DiscordGuild_DiscordId == guild.Id && i.Success == true && i.DiscordMessage_DiscordId != null);
                    var latestNotification = notifications.OrderByDescending(i => i.TimeStamp).FirstOrDefault();

                    var guildUser = guild.GetUser(user.Id);

                    var guildEmbedBuilder = new EmbedBuilder()
                        .WithColor(Color.Green)
                        .WithAuthor(user)
                        .WithDescription(guild.Name)
                        .WithThumbnailUrl(guild.IconUrl)
                        .WithFooter(guild.Id.ToString());

                    string userPermissions = "";
                    if (guild.OwnerId == user.Id)
                        userPermissions = "Owner";
                    else if (guildUser.GuildPermissions.Administrator)
                        userPermissions = "Administrator";
                    else if (guildUser.GuildPermissions.ManageGuild)
                        userPermissions = "Manage Guild";
                    else
                    {
                        var adminRoleId = discordGuild.Config?.AdminRoleDiscordId;
                        if (adminRoleId != null)
                            if (guildUser.Roles.Where(i => i.Id == adminRoleId).Any())
                                userPermissions = "Admin Role";
                    }
                    if (string.IsNullOrWhiteSpace(userPermissions))
                        continue;

                    TimestampTag? notificationTimestampTag = null;
                    if (latestNotification != null)
                        notificationTimestampTag = TimestampTag.FromDateTime(latestNotification.TimeStamp, TimestampTagStyles.Relative);

                    guildEmbedBuilder
                        .AddField(name: "Creation Date", value: $"{TimestampTag.FromDateTime(guild.CreatedAt.UtcDateTime, TimestampTagStyles.Relative)}", inline: true)
                        .AddField(name: "Subscription Count", value: $"{discordGuild.StreamSubscriptions.Count}", inline: true)
                        .AddField(name: "Notification Count", value: $"{notifications.Count()}", inline: true)
                        .AddField(name: "Latest Notification", value: $"{latestNotification?.ServiceType} {latestNotification?.User_Username ?? "none"} {(notificationTimestampTag?.ToString() ?? "unknown")}", inline: true)
                        .AddField(name: "Has Permission", value: $"{userPermissions}", inline: true);

                    guildEmbeds.Add(guildEmbedBuilder.Build());
                }
                if (guildEmbeds.Any())
                    await FollowupAsync(embeds: guildEmbeds.ToArray(), ephemeral: true);
            }
        }

        #endregion User Info Command

        #region Stats command

        [SlashCommand(name: "stats", description: "Get some general statistics of the bot")]
        public async Task StatsCommandAsync()
        {
            var appInfo = await Context.Client.GetApplicationInfoAsync();
            var subscriptions = await work.SubscriptionRepository.GetAllAsync();

            var reportPeriod = DateTime.UtcNow.Subtract(TimeSpan.FromDays(30));
            var notifications = await work.NotificationRepository.FindAsync(n => n.Success == true && n.TimeStamp >= reportPeriod);

            var embedBuilder = new EmbedBuilder()
                .WithAuthor(appInfo.Owner)
                .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl())
                .WithColor(Color.Green);

            embedBuilder.AddField(name: "Shard Count", value: $"{Context.Client.Shards.Count}", inline: true);
            embedBuilder.AddField(name: "Guild Count", value: $"{Context.Client.Guilds.Count}", inline: true);
            embedBuilder.AddField(name: "Last 30 days", value: $"{notifications.LongCount()}", inline: true);
            embedBuilder.AddField(name: "Subscriptions", value: $"{subscriptions.Count()}", inline: true);
            embedBuilder.AddField(name: "Unique Users", value: $"{subscriptions.Select(i => i.User).Distinct().Count()}", inline: true);

            Enum.GetValues<ServiceEnum>()
                .Where(i => i != ServiceEnum.None)
                .ToList()
                .ForEach(service =>
            {
                if (subscriptions.Any(i => i.User.ServiceType == service))
                    embedBuilder.AddField(name: $"{service} Subscriptions", value: $"{subscriptions.Count(i => i.User.ServiceType == service)}", inline: true);
            });

            await FollowupAsync(text: "General bot statistics", embed: embedBuilder.Build(), ephemeral: true);
        }

        #endregion Stats command

        #region Url Parse test

        [SlashCommand(name: "parse", description: "Used to test a URL and try to locate a monitor")]
        public async Task MonitorTestAsync(
            [Summary(name: "url", description: "The URL to parse")] Uri uri)
        {
            var monitor = monitors.Where(x => x.IsValid(uri.AbsoluteUri)).FirstOrDefault();
            var serviceType = uri.ToServiceEnum();
            var monitorBase = monitor?.BaseURL == null ? Format.Bold("unknown") : Format.EscapeUrl(monitor.BaseURL);

            var embed = new EmbedBuilder()
                .WithColor(serviceType.GetAlertColor())
                .WithDescription($"Service Type {Format.Bold(serviceType.ToString())} is {Format.Bold(((monitor?.IsEnabled ?? false) ? "enabled" : "disabled"))} with Base URL {monitorBase}")
                .Build();
            await FollowupAsync(embed: embed, ephemeral: true);
        }

        #endregion Url Parse test
    }
}
