using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    [DontAutoLoad]
    [RequireOwner]
    [Group("admin")]
    public class AdminModule : ModuleBase<ShardedCommandContext>
    {
        private readonly IUnitOfWork _work;

        public AdminModule(IUnitOfWorkFactory factory)
        {
            _work = factory.Create();
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
            var discordChannels = (await _work.SubscriptionRepository.GetAllAsync()).Select(i => i.DiscordChannel).Distinct();
            discordChannels.ToList().ForEach(i => Task.Run(() => _ProcessAlert(i, message)));
        }

        /// <summary>
        /// Task meant to be spawned from <see cref="SendAlertAsync(string)"/>
        /// </summary>
        /// <param name="discordChannel"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task _ProcessAlert(DiscordChannel discordChannel, string message)
        {
            SocketTextChannel channel = (SocketTextChannel)Context.Client.GetChannel(discordChannel.DiscordId);
            await channel.SendMessageAsync($"{message}");
        }
    }
}