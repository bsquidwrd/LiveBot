using Discord;
using Discord.Interactions;
using Discord.Rest;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;

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
            var IsDebug = configuration.GetValue<bool>("IsDebug", false);
            var testGuildId = configuration.GetValue<ulong>("testguild");

            if (IsDebug)
                await commands.RegisterCommandsToGuildAsync(guildId: testGuildId, deleteMissing: true);
            else
                await commands.RegisterCommandsGloballyAsync(deleteMissing: true);

            var adminGuild = await commands.RestClient.GetGuildAsync(testGuildId);
            await commands.AddModulesToGuildAsync(guild: adminGuild, deleteMissing: false, modules: commands.GetModuleInfo<AdminModule>());

            await FollowupAsync(text: "Finished registering commands", ephemeral: true);
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
    }
}