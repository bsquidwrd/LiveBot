using Discord;
using Discord.Interactions;
using Discord.Rest;
using LiveBot.Core.Repository.Static;

namespace LiveBot.Discord.SlashCommands.Modules
{
    public class GeneralModule : RestInteractionModuleBase<RestInteractionContext>
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
        [SlashCommand(name: "perm-check", description: "Confirm the bot has the necessary permissions to post in a channel")]
        public async Task PermCheckAsync(ITextChannel? channel = null)
        {
            if (channel == null)
                channel = (ITextChannel)Context.Channel;

            var guildUser = await Context.Guild.GetCurrentUserAsync();
            var perms = guildUser.GetPermissions(channel);

            var yesEmoji = new Emoji("\uD83D\uDFE9");
            var noEmoji = new Emoji("\uD83D\uDFE5");

            var embedBuilder = new EmbedBuilder()
                .WithColor(color: Color.Blue)
                .WithDescription($"Permissions for {channel.Mention}\n{yesEmoji} = Set\n{noEmoji} = Not Set");

            var viewChannelField = new EmbedFieldBuilder()
                .WithName("View Channel")
                .WithValue(perms.ViewChannel ? yesEmoji : noEmoji)
                .WithIsInline(true);
            embedBuilder.AddField(viewChannelField);

            var sendMessagesField = new EmbedFieldBuilder()
                .WithName("Send Messages")
                .WithValue(perms.SendMessages ? yesEmoji : noEmoji)
                .WithIsInline(true);
            embedBuilder.AddField(sendMessagesField);

            var embedLinksField = new EmbedFieldBuilder()
                .WithName("Embed Links")
                .WithValue(perms.EmbedLinks ? yesEmoji : noEmoji)
                .WithIsInline(true);
            embedBuilder.AddField(embedLinksField);

            var useExternalEmojisField = new EmbedFieldBuilder()
                .WithName("Use External Emojis")
                .WithValue(perms.UseExternalEmojis ? yesEmoji : noEmoji)
                .WithIsInline(true);
            embedBuilder.AddField(useExternalEmojisField);

            var readMessageHistoryField = new EmbedFieldBuilder()
                .WithName("Read Message History")
                .WithValue(perms.ReadMessageHistory ? yesEmoji : noEmoji)
                .WithIsInline(true);
            embedBuilder.AddField(readMessageHistoryField);

            var mentionEveryoneField = new EmbedFieldBuilder()
                .WithName("Mention Everyone")
                .WithValue(perms.MentionEveryone ? yesEmoji : noEmoji)
                .WithIsInline(true);
            embedBuilder.AddField(mentionEveryoneField);

            await FollowupAsync(text: "", embed: embedBuilder.Build(), ephemeral: true);
        }

        #endregion Perms Check command
    }
}