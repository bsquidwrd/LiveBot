using LiveBot.Core.Repository.Models;
using LiveBot.Core.Repository.Models.Discord;
using LiveBot.Core.Repository.Models.Streams;
using Microsoft.EntityFrameworkCore;

namespace LiveBot.Repository
{
    /// <inheritdoc/>
    public class LiveBotDBContext : DbContext
    {
        private readonly string _connectionstring;

        public LiveBotDBContext(string connectionstring) : base()
        {
            _connectionstring = connectionstring;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseLazyLoadingProxies()
                .UseNpgsql(_connectionstring);
        }

        public DbSet<DiscordGuild> DiscordGuild { get; set; }
        public DbSet<DiscordChannel> DiscordChannel { get; set; }
        public DbSet<DiscordRole> DiscordRole { get; set; }
        public DbSet<StreamSubscription> StreamSubscription { get; set; }
        public DbSet<StreamUser> StreamUser { get; set; }
        public DbSet<StreamNotification> StreamNotification { get; set; }
        public DbSet<StreamGame> StreamGame { get; set; }
        public DbSet<MonitorAuth> MonitorAuth { get; set; }
        public DbSet<DiscordGuildConfig> DiscordGuildConfig { get; set; }
    }
}