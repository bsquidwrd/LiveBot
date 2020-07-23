using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using LiveBot.Core.Repository.Static;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [Group("config")]
    [Summary("Monitor actions for Streams")]
    public class ConfigModule : InteractiveBase<ShardedCommandContext>
    {
        private readonly IUnitOfWork _work;

        /// <summary>
        /// Represents the Command to configure the bot for a Guild
        /// </summary>
        /// <param name="factory"></param>
        public ConfigModule(IUnitOfWorkFactory factory)
        {
            _work = factory.Create();
        }

        #region Commands

        /// <summary>
        /// Start the process of setting up a Guilds Config
        /// </summary>
        /// <returns></returns>
        [Command("setup", RunMode = RunMode.Async)]
        public async Task SetupConfig()
        {
            DiscordGuild discordGuild = await _GetDiscordGuild();
            DiscordGuildConfig guildConfig = await _GetDiscordGuildConfig(discordGuild);

            var notificationChannel = await _RequestNotificationChannel(discordGuild);
            var notificationMessage = await _RequestNotificationMessage();
            var notificationRole = await _RequestNotificationRole(discordGuild);
            var monitorRole = await _RequestMonitorRole(discordGuild);

            try
            {
                guildConfig.DiscordChannel = notificationChannel;
                guildConfig.Message = notificationMessage;
                guildConfig.DiscordRole = notificationRole;
                guildConfig.MonitorRole = monitorRole;
                await _work.GuildConfigRepository.UpdateAsync(guildConfig);

                guildConfig = await _GetDiscordGuildConfig(discordGuild);
                await _ReplyFinished(guildConfig);
            }
            catch (Exception e)
            {
                Log.Error($"Error running SetupConfig for {Context.Message.Author.Id} {Context.Message.Author.Username}#{Context.Message.Author.Discriminator} GuildID: {Context.Guild.Id} ChannelID: {Context.Channel.Id}\n{e}");
                await ReplyAsync($"{Context.Message.Author.Mention}, I wasn't able to setup the config for your server. Please try again or contact my owner");
            }
        }

        /// <summary>
        /// Configure Notification Channel
        /// </summary>
        /// <returns></returns>
        [Command("channel", RunMode = RunMode.Async)]
        public async Task SetupConfigChannel()
        {
            DiscordGuild discordGuild = await _GetDiscordGuild();
            DiscordGuildConfig guildConfig = await _GetDiscordGuildConfig(discordGuild);

            var notificationChannel = await _RequestNotificationChannel(discordGuild);

            try
            {
                guildConfig.DiscordChannel = notificationChannel;
                await _work.GuildConfigRepository.UpdateAsync(guildConfig);

                guildConfig = await _GetDiscordGuildConfig(discordGuild);
                await _ReplyFinished(guildConfig);
            }
            catch (Exception e)
            {
                Log.Error($"Error running SetupConfig for {Context.Message.Author.Id} {Context.Message.Author.Username}#{Context.Message.Author.Discriminator} GuildID: {Context.Guild.Id} ChannelID: {Context.Channel.Id}\n{e}");
                await ReplyAsync($"{Context.Message.Author.Mention}, I wasn't able to setup the config for your server. Please try again or contact my owner");
            }
        }

        /// <summary>
        /// Configure Notification Message
        /// </summary>
        /// <returns></returns>
        [Command("message", RunMode = RunMode.Async)]
        public async Task SetupConfigMessage()
        {
            DiscordGuild discordGuild = await _GetDiscordGuild();
            DiscordGuildConfig guildConfig = await _GetDiscordGuildConfig(discordGuild);

            var notificationMessage = await _RequestNotificationMessage();

            try
            {
                guildConfig.Message = notificationMessage;
                await _work.GuildConfigRepository.UpdateAsync(guildConfig);

                guildConfig = await _GetDiscordGuildConfig(discordGuild);
                await _ReplyFinished(guildConfig);
            }
            catch (Exception e)
            {
                Log.Error($"Error running SetupConfig for {Context.Message.Author.Id} {Context.Message.Author.Username}#{Context.Message.Author.Discriminator} GuildID: {Context.Guild.Id} ChannelID: {Context.Channel.Id}\n{e}");
                await ReplyAsync($"{Context.Message.Author.Mention}, I wasn't able to setup the config for your server. Please try again or contact my owner");
            }
        }

        /// <summary>
        /// Configure Role to monitor
        /// </summary>
        /// <returns></returns>
        [Command("monitor", RunMode = RunMode.Async)]
        public async Task SetupConfigMonitor()
        {
            DiscordGuild discordGuild = await _GetDiscordGuild();
            DiscordGuildConfig guildConfig = await _GetDiscordGuildConfig(discordGuild);

            var monitorRole = await _RequestMonitorRole(discordGuild);

            try
            {
                guildConfig.MonitorRole = monitorRole;
                await _work.GuildConfigRepository.UpdateAsync(guildConfig);

                guildConfig = await _GetDiscordGuildConfig(discordGuild);
                await _ReplyFinished(guildConfig);
            }
            catch (Exception e)
            {
                Log.Error($"Error running SetupConfig for {Context.Message.Author.Id} {Context.Message.Author.Username}#{Context.Message.Author.Discriminator} GuildID: {Context.Guild.Id} ChannelID: {Context.Channel.Id}\n{e}");
                await ReplyAsync($"{Context.Message.Author.Mention}, I wasn't able to setup the config for your server. Please try again or contact my owner");
            }
        }

        /// <summary>
        /// Configure Role to mention
        /// </summary>
        /// <returns></returns>
        [Command("mention", RunMode = RunMode.Async)]
        public async Task SetupConfigMention()
        {
            DiscordGuild discordGuild = await _GetDiscordGuild();
            DiscordGuildConfig guildConfig = await _GetDiscordGuildConfig(discordGuild);

            var notificationRole = await _RequestNotificationRole(discordGuild);

            try
            {
                guildConfig.DiscordRole = notificationRole;
                await _work.GuildConfigRepository.UpdateAsync(guildConfig);

                guildConfig = await _GetDiscordGuildConfig(discordGuild);
                await _ReplyFinished(guildConfig);
            }
            catch (Exception e)
            {
                Log.Error($"Error running SetupConfig for {Context.Message.Author.Id} {Context.Message.Author.Username}#{Context.Message.Author.Discriminator} GuildID: {Context.Guild.Id} ChannelID: {Context.Channel.Id}\n{e}");
                await ReplyAsync($"{Context.Message.Author.Mention}, I wasn't able to setup the config for your server. Please try again or contact my owner");
            }
        }

        #endregion Commands

        #region Helpers

        private async Task _ReplyFinished(DiscordGuildConfig guildConfig)
        {
            string channelName = guildConfig.DiscordChannel != null ? MentionUtils.MentionChannel(guildConfig.DiscordChannel.DiscordId) : "`none`";
            string monitorRoleName = guildConfig.MonitorRole?.Name ?? "none";
            string mentionRoleName = guildConfig.DiscordRole?.Name ?? "none";

            if (monitorRoleName == "@everyone")
                monitorRoleName = "everyone";
            if (mentionRoleName == "@everyone")
                mentionRoleName = "everyone";

            await ReplyAsync($"{Context.Message.Author.Mention}, I have configured your server to monitor the role `{monitorRoleName}` and post in {channelName} with the message `{guildConfig.Message}` mentioning `{mentionRoleName}`");
        }

        /// <summary>
        /// Simple method to delete a message and not fail
        /// </summary>
        /// <param name="message"></param>
        private async Task _DeleteMessage(IMessage message)
        {
            try
            {
                await message.DeleteAsync();
            }
            catch
            { }
        }

        /// <summary>
        /// Get the Discord Guild for this Context
        /// </summary>
        /// <returns></returns>
        public async Task<DiscordGuild> _GetDiscordGuild()
        {
            DiscordGuild discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(g => g.DiscordId == Context.Guild.Id);
            if (discordGuild == null)
            {
                DiscordGuild newDiscordGuild = new DiscordGuild
                {
                    DiscordId = Context.Guild.Id,
                    Name = Context.Guild.Name,
                    IconUrl = Context.Guild.IconUrl
                };
                await _work.GuildRepository.AddOrUpdateAsync(newDiscordGuild, g => g.DiscordId == Context.Guild.Id);
                discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(g => g.DiscordId == Context.Guild.Id);
            }
            return discordGuild;
        }

        /// <summary>
        /// Get <seealso cref="DiscordGuildConfig"/> for the provided <paramref name="discordGuild"/>
        /// </summary>
        /// <param name="discordGuild"></param>
        /// <returns></returns>
        public async Task<DiscordGuildConfig> _GetDiscordGuildConfig(DiscordGuild discordGuild)
        {
            // Get/Create Guild Config
            DiscordGuildConfig guildConfig = discordGuild.Config;
            if (guildConfig is null)
            {
                var newGuildConfig = new DiscordGuildConfig
                {
                    DiscordGuild = discordGuild
                };
                await _work.GuildConfigRepository.AddOrUpdateAsync(newGuildConfig, i => i.DiscordGuild == discordGuild);
                guildConfig = await _work.GuildConfigRepository.SingleOrDefaultAsync(i => i.DiscordGuild == discordGuild);
                discordGuild.Config = guildConfig;
                await _work.GuildRepository.UpdateAsync(discordGuild);
            }
            return guildConfig;
        }

        /// <summary>
        /// Helper Function to simplify asking for a channel
        /// </summary>
        /// <returns>Resulting Channel object from the Users input</returns>
        private async Task<DiscordChannel> _RequestNotificationChannel(DiscordGuild discordGuild)
        {
            var messageEmbedFooter = new EmbedFooterBuilder()
                .WithText("Please mention the channel with the # prefix");
            var messageEmbed = new EmbedBuilder()
                .WithColor(Color.DarkPurple)
                .WithDescription($"Please mention the Discord Channel you would like to start or stop a notification for.\nEx: {MentionUtils.MentionChannel(Context.Channel.Id)}\nYou can also type `none` if you'd like to stop me from posting")
                .WithFooter(messageEmbedFooter)
                .Build();

            var questionMessage = await ReplyAsync(message: $"{Context.Message.Author.Mention}", embed: messageEmbed);
            var responseMessage = await NextMessageAsync(timeout: Defaults.MessageTimeout);

            await _DeleteMessage(questionMessage);
            await _DeleteMessage(responseMessage);

            if (responseMessage.Content.Trim().Equals("none", StringComparison.CurrentCultureIgnoreCase))
                return null;

            IGuildChannel guildChannel = responseMessage.MentionedChannels.FirstOrDefault();

            DiscordChannel discordChannel = null;
            if (guildChannel != null)
            {
                discordChannel = new DiscordChannel
                {
                    DiscordGuild = discordGuild,
                    DiscordId = guildChannel.Id,
                    Name = guildChannel.Name
                };
                await _work.ChannelRepository.AddOrUpdateAsync(discordChannel, i => i.DiscordGuild == discordGuild && i.DiscordId == guildChannel.Id);
                discordChannel = await _work.ChannelRepository.SingleOrDefaultAsync(c => c.DiscordId == guildChannel.Id);
            }

            return discordChannel;
        }

        /// <summary>
        /// Helper Function to simplify asking for a Role to mention
        /// </summary>
        /// <returns></returns>
        private async Task<DiscordRole> _RequestNotificationRole(DiscordGuild discordGuild)
        {
            var messageEmbedFooter = new EmbedFooterBuilder()
                .WithText("You do NOT have to mention the role with the @ symbol");
            var messageEmbed = new EmbedBuilder()
                .WithColor(Color.DarkPurple)
                .WithDescription($"What is the name of the Role you would like to mention in messages?\nEx: `{Context.Guild.CurrentUser.Roles.First(d => d.IsEveryone == false).Name}`, `everyone` or `none` if you don't want to ping a role")
                .WithFooter(messageEmbedFooter)
                .Build();

            var questionMessage = await ReplyAsync(message: $"{Context.Message.Author.Mention}", embed: messageEmbed);
            var responseMessage = await NextMessageAsync(timeout: Defaults.MessageTimeout);
            IRole role = responseMessage.MentionedRoles.FirstOrDefault();
            if (role == null)
            {
                string response = responseMessage.Content.Trim();
                if (response.Equals("everyone", StringComparison.CurrentCultureIgnoreCase))
                {
                    role = Context.Guild.EveryoneRole;
                }
                else if (response.Equals("none", StringComparison.CurrentCultureIgnoreCase))
                {
                    role = null;
                }
                else
                {
                    role = Context.Guild.Roles.FirstOrDefault(d => d.Name.Equals(response, StringComparison.CurrentCultureIgnoreCase));
                }
            }
            await _DeleteMessage(questionMessage);
            await _DeleteMessage(responseMessage);

            DiscordRole discordRole = null;
            if (role != null)
            {
                discordRole = new DiscordRole
                {
                    DiscordGuild = discordGuild,
                    DiscordId = role.Id,
                    Name = role.Name
                };
                await _work.RoleRepository.AddOrUpdateAsync(discordRole, i => i.DiscordGuild == discordGuild && i.DiscordId == role.Id);
                discordRole = await _work.RoleRepository.SingleOrDefaultAsync(r => r.DiscordId == role.Id);
            }

            return discordRole;
        }

        /// <summary>
        /// Helper Function to simplify asking for a Role to mention
        /// </summary>
        /// <returns></returns>
        private async Task<DiscordRole> _RequestMonitorRole(DiscordGuild discordGuild)
        {
            var messageEmbedFooter = new EmbedFooterBuilder()
                .WithText("You do NOT have to mention the role with the @ symbol");
            var messageEmbed = new EmbedBuilder()
                .WithColor(Color.DarkPurple)
                .WithDescription($"What is the name of the Role you would like to monitor?\nEx: `{Context.Guild.CurrentUser.Roles.First(d => d.IsEveryone == false).Name}`, `everyone` or `none` if you don't want to monitor a role")
                .WithFooter(messageEmbedFooter)
                .Build();

            var questionMessage = await ReplyAsync(message: $"{Context.Message.Author.Mention}", embed: messageEmbed);
            var responseMessage = await NextMessageAsync(timeout: Defaults.MessageTimeout);
            IRole role = responseMessage.MentionedRoles.FirstOrDefault();
            if (role == null)
            {
                string response = responseMessage.Content.Trim();
                if (response.Equals("everyone", StringComparison.CurrentCultureIgnoreCase))
                {
                    role = Context.Guild.EveryoneRole;
                }
                else if (response.Equals("none", StringComparison.CurrentCultureIgnoreCase))
                {
                    role = null;
                }
                else
                {
                    role = Context.Guild.Roles.FirstOrDefault(d => d.Name.Equals(response, StringComparison.CurrentCultureIgnoreCase));
                }
            }
            await _DeleteMessage(questionMessage);
            await _DeleteMessage(responseMessage);

            DiscordRole discordRole = null;
            if (role != null)
            {
                discordRole = new DiscordRole
                {
                    DiscordGuild = discordGuild,
                    DiscordId = role.Id,
                    Name = role.Name
                };
                await _work.RoleRepository.AddOrUpdateAsync(discordRole, i => i.DiscordGuild == discordGuild && i.DiscordId == role.Id);
                discordRole = await _work.RoleRepository.SingleOrDefaultAsync(r => r.DiscordId == role.Id);
            }

            return discordRole;
        }

        private async Task<string> _RequestNotificationMessage()
        {
            string parameters = "";
            parameters += "{role} - Role to ping (if applicable)\n";
            parameters += "{name} - Streamers Name\n";
            parameters += "{game} - Game they are playing\n";
            parameters += "{url} - URL to the stream\n";
            parameters += "{title} - Stream Title\n";

            var embedFieldBuilder = new EmbedFieldBuilder()
                .WithIsInline(true)
                .WithName("Parameters")
                .WithValue(parameters);

            var messageEmbedFooter = new EmbedFooterBuilder()
                .WithText($"Default: {Defaults.NotificationMessage}");
            var messageEmbed = new EmbedBuilder()
                .WithColor(Color.DarkPurple)
                .WithDescription("What message should be sent? (Max 255 characters)\nIf you'd like to use the default (see footer) type default")
                .WithFields(embedFieldBuilder)
                .WithFooter(messageEmbedFooter)
                .Build();

            var questionMessage = await ReplyAsync(message: $"{Context.Message.Author.Mention}", embed: messageEmbed);
            var responseMessage = await NextMessageAsync(timeout: Defaults.MessageTimeout);
            string notificationMessage = responseMessage.Content.Trim();

            if (notificationMessage.Equals("default", StringComparison.CurrentCultureIgnoreCase))
            {
                notificationMessage = Defaults.NotificationMessage;
            }

            await _DeleteMessage(questionMessage);
            await _DeleteMessage(responseMessage);
            return notificationMessage;
        }

        #endregion Helpers
    }
}