﻿using Discord;
using Discord.Commands;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Discord.Contracts;
using MassTransit;
using Serilog;
using System;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    [RequireOwner]
    [Group("admin")]
    public class AdminModule : ModuleBase<ShardedCommandContext>
    {
        private readonly IUnitOfWork _work;
        private readonly IBusControl _bus;

        public AdminModule(IUnitOfWorkFactory factory, IBusControl bus)
        {
            _work = factory.Create();
            _bus = bus;
        }

        /// <summary>
        /// Allow the Owner of the bot to restart all the services attached to it
        /// </summary>
        /// <returns></returns>
        [RequireOwner]
        [Command("restart")]
        [Remarks("Restart the bot and all attached services")]
        public async Task RestartBotAsync()
        {
            await Context.Client.SetStatusAsync(UserStatus.Invisible);

            var msg = $"{Context.User.Mention}, I am restarting. Enjoy the silence, you monster!";
            Embed embed = new EmbedBuilder().WithImageUrl("https://i.imgur.com/XSi0zrl.png").Build();
            await ReplyAsync(message: msg, embed: embed);
            Log.Information($"Restart initiated by {Context.Message.Author.Username} in {Context.Guild.Name} ({Context.Guild.Id})");

            await Context.Client.StopAsync();
            Environment.Exit(0);
        }

        /// <summary>
        /// Process and Send an alert to all distinct channels that have subscriptions
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [RequireOwner]
        [Command("alert", RunMode = RunMode.Async)]
        [Remarks("Send an alert to all Discord Channels that have an Active Subscription")]
        public async Task SendAlertAsync(string message)
        {
            var payload = new DiscordAlert { Message = message };
            await _bus.Publish(payload);
            await ReplyAsync($"{Context.Message.Author.Mention}, I have queued the message to be sent.");
        }

        [RequireOwner]
        [Command("beta")]
        [Remarks("Get status of Beta for a Guild")]
        public async Task GetBetaStatus()
        {
            var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == Context.Guild.Id);
            await ReplyAsync($"{Context.Message.Author.Mention}, The Beta status of this Guild is `{discordGuild.IsInBeta}`");
        }

        [RequireOwner]
        [Command("beta")]
        [Remarks("Toggle the Beta status of a Guild")]
        public async Task ToggleBeta(bool status)
        {
            var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == Context.Guild.Id);
            discordGuild.IsInBeta = status;
            await _work.GuildRepository.UpdateAsync(discordGuild);
            await ReplyAsync($"{Context.Message.Author.Mention}, I have set the Beta status of this Guild to `{discordGuild.IsInBeta}`");
        }

        [RequireOwner]
        [Command("cleanup")]
        [Remarks("Cleanup the DB of Guilds the bot has left")]
        public async Task CleanupDatabase()
        {
            var guilds = await _work.GuildRepository.GetAllAsync();
            foreach (var guild in guilds)
            {
                if (Context.Client.GetGuild(guild.DiscordId) == null)
                    await _bus.Publish(new DiscordGuildDelete
                    {
                        GuildId = guild.DiscordId
                    });
            }
            await ReplyAsync($"{Context.Message.Author.Mention}, I have queued a cleanup of all Guilds");
        }
    }
}