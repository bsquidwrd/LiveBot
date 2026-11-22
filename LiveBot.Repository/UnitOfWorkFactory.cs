using System;
using LiveBot.Core.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LiveBot.Repository
{
    /// <inheritdoc/>
    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly IDbContextFactory<LiveBotDBContext> _contextFactory;

        public UnitOfWorkFactory(IDbContextFactory<LiveBotDBContext> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        /// <inheritdoc/>
        public void Migrate()
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                // Index builds on StreamNotification run for minutes; the 30s default would cancel
                // them partway and leave an invalid index behind.
                context.Database.SetCommandTimeout(TimeSpan.FromHours(1));
                try
                {
                    context.Database.Migrate();
                }
                finally
                {
                    // The instance goes back to the pool, and later leases must not inherit the hour.
                    context.Database.SetCommandTimeout(null);
                }
            }
        }

        /// <inheritdoc/>
        public IUnitOfWork Create()
        {
            return new UnitOfWork(_contextFactory.CreateDbContext());
        }
    }
}
