﻿using Discord;
using Discord.Commands;
using Interactivity;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Static;
using System;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    [RequireContext(ContextType.Guild)]
    public class PublicModule : ModuleBase<ShardedCommandContext>
    {
        private readonly IUnitOfWork _work;
        private readonly InteractivityService _interactivity;

        public PublicModule(IUnitOfWorkFactory factory, InteractivityService interactivity)
        {
            _work = factory.Create();
            _interactivity = interactivity;
        }

        /// <summary>
        /// Gets general information about the bot
        /// </summary>
        /// <returns></returns>
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        [Command("info")]
        public async Task InfoAsync()
        {
            var AppInfo = await Context.Client.GetApplicationInfoAsync();
            var ReplyEmbed = new EmbedBuilder()
                .WithTitle($"{AppInfo.Name} Information")
                //.WithDescription($"")
                .WithUrl(Basic.WebsiteLink)
                .WithColor(Color.DarkPurple)
                .WithAuthor(AppInfo.Owner)
                .WithFooter(footer => footer.Text = $"Shard {Context.Client.GetShardFor(Context.Guild).ShardId + 1} / {Context.Client.Shards.Count}")
                .WithCurrentTimestamp()
                .Build();
            await ReplyAsync(null, false, ReplyEmbed);
        }

        /// <summary>
        /// General command to test that the bot can send messages, as well as a cheeky way of
        /// getting my name out there
        /// </summary>
        /// <returns></returns>
        [Command("hello")]
        public async Task HelloAsync()
        {
            var AppInfo = await Context.Client.GetApplicationInfoAsync();
            var msg = $"Hello, I am a bot created by {AppInfo.Owner.Username}#{AppInfo.Owner.DiscriminatorValue}";
            await ReplyAsync(msg);
        }

        /// <summary>
        /// Provides a link to the GitHub Repository for the bot
        /// </summary>
        /// <returns></returns>
        [Command("source")]
        public async Task SourceAsync()
        {
            _interactivity.DelayedSendMessageAndDeleteAsync(Context.Channel, text: $"{Context.Message.Author.Mention} You can find my source code here: {Basic.SourceLink}", deleteDelay: TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Provides a Discord Server invite to my Support Server
        /// </summary>
        /// <returns></returns>
        [Command("support")]
        public async Task SupportAsync()
        {
            _interactivity.DelayedSendMessageAndDeleteAsync(Context.Channel, text: $"{Context.Message.Author.Mention}, you can find my support server here: {Basic.SupportInvite}", deleteDelay: TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Provides a link on how to support me via PayPal
        /// </summary>
        /// <returns></returns>
        [Command("donate")]
        public async Task DonateAsync()
        {
            _interactivity.DelayedSendMessageAndDeleteAsync(Context.Channel, text: $"{Context.Message.Author.Mention}, Thank you so much for even considering donating! You can donate here: <{Basic.DonationLink}>", deleteDelay: TimeSpan.FromMinutes(1));
        }
    }
}