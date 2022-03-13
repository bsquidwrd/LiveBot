using LiveBot.Core.Repository.Interfaces;
using LiveBot.Repository;

namespace LiveBot.Watcher.Twitch
{
    public static class DatabaseSetup
    {
        /// <summary>
        /// Adds EnvironmentVariables to config, configures and adds
        /// <see cref="DiscordRestClient"/> and <see cref="InteractionService"/>
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static WebApplicationBuilder SetupLiveBotDatabase(this WebApplicationBuilder builder)
        {
            builder.Configuration.AddEnvironmentVariables(prefix: "LiveBot_");

            builder.Services.AddSingleton<IUnitOfWorkFactory>(new UnitOfWorkFactory(builder.Configuration));

            // Setup MassTransit
            builder.Services.AddLiveBotQueueing();

            return builder;
        }
    }
}