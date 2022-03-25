using Discord;
using Discord.Interactions;
using Discord.Rest;

namespace LiveBot.Discord.SlashCommands.Modules
{
    public class GeneralModule : RestInteractionModuleBase<RestInteractionContext>
    {
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

            var missingPermissions = new List<string>();

            if (!perms.ViewChannel)
                missingPermissions.Add("View Channel");
            if (!perms.SendMessages)
                missingPermissions.Add("Send Messages");
            if (!perms.EmbedLinks)
                missingPermissions.Add("Embed Links");
            if (!perms.UseExternalEmojis)
                missingPermissions.Add("Use External Emojis");
            if (!perms.ReadMessageHistory)
                missingPermissions.Add("Read Message History");
            if (!perms.MentionEveryone)
                missingPermissions.Add("Mention Everyone");

            var permissionsResult = "All permissions are set correctly!";
            if (missingPermissions.Any())
                permissionsResult = $"Missing Permissions in {channel.Mention}: {string.Join(", ", missingPermissions)}";
            await FollowupAsync(text: permissionsResult, ephemeral: true);
        }
    }
}