using Discord;
using Discord.Interactions;
using Discord.Rest;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using LiveBot.Core.Repository.Static;
using System.Globalization;

namespace LiveBot.Discord.SlashCommands.Modules
{
    [RequireOwner]
    [DontAutoRegister]
    [Group(name: "admin", description: "Administrative functions for the Bot Owner")]
    public class AdminModule : RestInteractionModuleBase<RestInteractionContext>
    {
        private readonly ILogger<AdminModule> logger;
        private readonly IConfiguration configuration;
        private readonly InteractionService commands;
        private readonly IUnitOfWork work;

        public AdminModule(ILogger<AdminModule> logger, IConfiguration configuration, InteractionService commands, IUnitOfWorkFactory factory)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.commands = commands;
            this.work = factory.Create();
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
            var IsDebug = configuration.GetValue<bool>("IsDebug", false);
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
            var guild = await Context.Client.GetGuildAsync(parsedGuildId);
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

            logger.LogInformation(message: "Set Beta status of {GuildName} ({GuildId}) to {BetaStatus}", discordGuild.Name, discordGuild.DiscordId, discordGuild.IsInBeta);

            await FollowupAsync(text: $"I have updated the beta status for {Format.Code(discordGuild.Name)} to {Format.Code(discordGuild.IsInBeta.ToString())}", ephemeral: true);
        }

        #endregion Beta command

        #region Info command

        /// <summary>
        /// Given an Id, get some basic information about the Guild that may help
        /// when there seems to be an issue with the bot
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [SlashCommand(name: "info", description: "Get misc information of a guild (used for troubleshooting)")]
        public async Task AdminInfoCommandAsync(
            [Summary(name: "guild-id", description: "The Guild Id to get information for")] string? id = null
        )
        {
            if (id == null)
                id = Context.Guild.Id.ToString();
            if (!ulong.TryParse(id, out var guildId))
                throw new Exception("Could not parse Guild Id");

            var guild = await Context.Client.GetGuildAsync(guildId);
            if (guild == null)
                throw new Exception($"Unable to find the Guild with Id {guildId}");

            var discordGuild = await work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == guild.Id);
            var notifications = await work.NotificationRepository.FindAsync(i => i.DiscordGuild_DiscordId == guild.Id && i.Success == true);
            var latestNotification = notifications.OrderByDescending(i => i.TimeStamp).FirstOrDefault();

            var embedBuilder = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithCurrentTimestamp()
                .WithAuthor(await guild.GetOwnerAsync())
                .WithThumbnailUrl(guild.IconUrl)
                .WithDescription($"");

            embedBuilder.AddField(name: "Creation Date", value: $"{guild.CreatedAt.UtcDateTime.ToString("u", CultureInfo.GetCultureInfo("en-US"))}", inline: true);
            embedBuilder.AddField(name: "Subscription Count", value: $"{discordGuild.StreamSubscriptions.Count}", inline: true);
            embedBuilder.AddField(name: "Notification Count", value: $"{notifications.Count()}", inline: true);
            embedBuilder.AddField(name: "Latest Notification", value: $"{latestNotification?.ServiceType} {latestNotification?.User_Username ?? "none"}", inline: true);

            foreach (var serviceType in notifications.Select(i => i.ServiceType).Distinct())
                embedBuilder.AddField(name: $"{serviceType} Subscriptions", value: notifications.Count(i => i.ServiceType == serviceType), inline: true);

            await FollowupAsync(text: $"Guild information for {Format.Bold(guild.Name)}", embed: embedBuilder.Build(), ephemeral: true);
        }

        #endregion Info command

        #region Stats command

        [SlashCommand(name: "stats", description: "Get some general statistics of the bot")]
        public async Task StatsCommandAsync()
        {
            var appInfo = await Context.Client.GetApplicationInfoAsync();
            var guilds = await work.GuildRepository.GetAllAsync();
            var subscriptions = await work.SubscriptionRepository.GetAllAsync();

            var embedBuilder = new EmbedBuilder()
                .WithAuthor(appInfo.Owner)
                .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl())
                .WithColor(Color.Green);

            embedBuilder.AddField(name: "Guild Count", value: $"{guilds.Count()}", inline: true);
            embedBuilder.AddField(name: "Subscriptions", value: $"{subscriptions.Count()}", inline: true);
            embedBuilder.AddField(name: "Unique Subscription Users", value: $"{subscriptions.Select(i => i.User).Distinct().Count()}", inline: true);

            Enum.GetValues<ServiceEnum>().ToList().ForEach(service =>
            {
                embedBuilder.AddField(name: $"{service} Subscriptions", value: $"{subscriptions.Count(i => i.User.ServiceType == service)}", inline: true);
            });

            await FollowupAsync(text: "General bot statistics", embed: embedBuilder.Build(), ephemeral: true);
        }

        #endregion Stats command
    }
}