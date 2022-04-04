using Discord;
using Discord.Interactions;
using LiveBot.Core.Repository.Static;
using LiveBot.Discord.SlashCommands.Attributes;

namespace LiveBot.Discord.SlashCommands.Modules
{
    public class GeneralModule : InteractionModuleBase<ShardedInteractionContext>
    {
        #region Misc commands

        [SlashCommand(name: "source-code", description: "Get a link to the Source Code for the bot")]
        public Task SourceCommandAsync() =>
            FollowupAsync(text: $"You can find my source code here: {Basic.SourceLink}", ephemeral: true);

        [SlashCommand(name: "support", description: "Get an invite link to the support server")]
        public Task SupportCommandAsync() =>
            FollowupAsync(text: $"You can find support for me here: {Basic.SupportInvite}", ephemeral: true);

        [SlashCommand(name: "tip", description: "Get information on how to tip for running the bot")]
        public Task TipCommandAsync() =>
            FollowupAsync(text: $"Thank you so much for thinking about tipping to help keep the bot running.\nTips are accepted via PayPal: {Basic.DonationLink}", ephemeral: true);

        #endregion Misc commands

        #region Perms Check command

        /// <summary>
        /// Check permissions against a channel (if provided)
        /// to ensure the bot has the appropriate permissions
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        [RequireBotManager]
        [SlashCommand(name: "perm-check", description: "Confirm the bot has the necessary permissions to post in a channel")]
        public async Task PermCheckAsync(ITextChannel? channel = null)
        {
            if (channel == null)
                channel = (ITextChannel)Context.Channel;

            var guildUser = Context.Guild.CurrentUser;
            var perms = guildUser.GetPermissions(channel);

            var yesEmoji = new Emoji("\uD83D\uDFE9");
            var noEmoji = new Emoji("\uD83D\uDFE5");

            var embedBuilder = new EmbedBuilder()
                .WithColor(color: Color.Blue)
                .WithDescription($"Permissions for {channel.Mention}\n{yesEmoji} = Set\n{noEmoji} = Not Set");

            embedBuilder.AddField(name: "View Channel", value: (perms.ViewChannel ? yesEmoji : noEmoji), inline: true);
            embedBuilder.AddField(name: "Send Messages", value: (perms.SendMessages ? yesEmoji : noEmoji), inline: true);
            embedBuilder.AddField(name: "Embed Links", value: (perms.EmbedLinks ? yesEmoji : noEmoji), inline: true);
            embedBuilder.AddField(name: "Use External Emojis", value: (perms.UseExternalEmojis ? yesEmoji : noEmoji), inline: true);
            embedBuilder.AddField(name: "Read Message History", value: (perms.ReadMessageHistory ? yesEmoji : noEmoji), inline: true);
            embedBuilder.AddField(name: "Mention Everyone", value: (perms.MentionEveryone ? yesEmoji : noEmoji), inline: true);

            await FollowupAsync(text: "", embed: embedBuilder.Build(), ephemeral: true);
        }

        #endregion Perms Check command
    }
}