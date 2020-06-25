using Discord;
using Discord.Rest;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LiveBot.API.Helpers
{
    public static class DiscordHelper
    {
        public static async Task<IEnumerable<RestUserGuild>> GetUserGuilds(HttpContext context)
        {
            var access_token = await context.GetTokenAsync("access_token");
            var clientConfig = new DiscordRestConfig()
            {
                LogLevel = LogSeverity.Debug,
                DefaultRetryMode = RetryMode.AlwaysRetry
            };
            var client = new DiscordRestClient(clientConfig);

            await client.LoginAsync(TokenType.Bearer, access_token).ConfigureAwait(false);

            while (client.LoginState != LoginState.LoggedIn)
            {
                await Task.Delay(1);
            }

            var summaryModels = await client.GetGuildSummariesAsync().FlattenAsync().ConfigureAwait(false);
            var userGuilds = summaryModels;

            await client.LogoutAsync();

            return userGuilds;
        }

        public static async Task<IEnumerable<RestUserGuild>> GetUserGuilds(HttpContext context, Func<RestUserGuild, bool> predicate)
            => (await GetUserGuilds(context)).Where(predicate);

        public static async Task<IEnumerable<RestUserGuild>> GetGuildsUserCanManage(HttpContext context)
            => await GetUserGuilds(context, i => i.Permissions.ManageGuild == true || i.Permissions.Administrator == true || i.IsOwner == true);

        public static async Task<IEnumerable<RestUserGuild>> GetGuildsUserCanManage(HttpContext context, Func<RestUserGuild, bool> predicate)
            => (await GetGuildsUserCanManage(context)).Where(predicate);
    }
}