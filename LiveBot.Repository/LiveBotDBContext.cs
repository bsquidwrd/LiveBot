using LiveBot.Core.Repository.Models.Discord;
using LiveBot.Core.Repository.Models.Streams;
using Microsoft.EntityFrameworkCore;
using System;

namespace LiveBot.Repository
{
    /// <inheritdoc/>
    public class LiveBotDBContext : DbContext
    {
        public LiveBotDBContext() : base()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(Environment.GetEnvironmentVariable("LiveBotConnectionString"));
        }

        public DbSet<DiscordGuild> DiscordGuild { get; set; }
        public DbSet<DiscordChannel> DiscordChannel { get; set; }
        public DbSet<DiscordRole> DiscordRole { get; set; }
        public DbSet<StreamSubscription> StreamSubscription { get; set; }
    }
}