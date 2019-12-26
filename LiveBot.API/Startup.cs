using Discord.Commands;
using Discord.WebSocket;
using LiveBot.Core.Repository;
using LiveBot.Discord.Services;
using LiveBot.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LiveBot.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // Add DiscordShardedClient
            var config = new DiscordSocketConfig
            {
                TotalShards = 1
            };

            services.AddSingleton(new DiscordShardedClient(config));
            services.AddSingleton<CommandService>();
            services.AddSingleton<CommandHandlingService>();

            // Add UnitOfWorkFactory
            var factory = new UnitOfWorkFactory();
            services.AddSingleton<IUnitOfWorkFactory>(factory);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}