using System;
using LiveBot.Core.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LiveBot.Repository
{
    public static class DatabaseServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a pooled <see cref="LiveBotDBContext"/> factory and the <see cref="IUnitOfWorkFactory"/> built on it
        /// </summary>
        public static IServiceCollection AddLiveBotDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("connectionstring");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException("connectionstring");

            services.AddPooledDbContextFactory<LiveBotDBContext>(options =>
                options
                    .UseLazyLoadingProxies()
                    .UseNpgsql(connectionString));
            services.AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>();

            return services;
        }
    }
}
