using System;
using LiveBot.Core.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LiveBot.Repository
{
    /// <inheritdoc/>
    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly string _connectionstring;
        private readonly DbContextOptions _options;

        public UnitOfWorkFactory(IConfiguration configuration)
        {
            _connectionstring = configuration.GetValue<string>("connectionstring");
            if (string.IsNullOrWhiteSpace(_connectionstring))
                throw new ArgumentNullException(nameof(_connectionstring));

            var optionBuilder = new DbContextOptionsBuilder();
            optionBuilder.UseNpgsql(_connectionstring);

            _options = optionBuilder.Options;
        }

        /// <inheritdoc/>
        public void Migrate()
        {
            using (var context = GetDbContext())
            {
                // Index builds on StreamNotification run for minutes; the 30s default would cancel
                // them partway and leave an invalid index behind.
                context.Database.SetCommandTimeout(TimeSpan.FromHours(1));
                context.Database.Migrate();
            }
        }

        /// <inheritdoc/>
        public IUnitOfWork Create()
        {
            return new UnitOfWork(GetDbContext());
        }

        private LiveBotDBContext GetDbContext()
        {
            return new LiveBotDBContext(_connectionstring);
        }
    }
}