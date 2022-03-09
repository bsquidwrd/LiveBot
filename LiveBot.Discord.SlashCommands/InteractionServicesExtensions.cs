using Discord;
using Discord.Interactions;
using Discord.Rest;

using DNetInteractions = Discord.Interactions;

namespace LiveBot.Discord.SlashCommands
{
    public static class WebApplicationBuilderExtensions
    {
        public static IApplicationBuilder MapInteractionService(this IApplicationBuilder builder, string path, string pbk)
        {
            builder.MapWhen(ctx => ctx.Request.Path == path && ctx.Request.Method == "POST", app => app.UseMiddleware<InteractionServiceMiddleware>(pbk));

            return builder;
        }

        public static IServiceCollection AddInteractionService(this IServiceCollection services, Action<InteractionServiceConfig> configure)
        {
            var config = new InteractionServiceConfig();
            configure(config);
            config.DefaultRunMode = RunMode.Sync;

            services.AddSingleton(config);
            services.AddSingleton<InteractionService>();

            return services;
        }
    }

    public sealed class InteractionServiceMiddleware
    {
        private readonly ILogger<InteractionServiceMiddleware> _logger;
        private readonly DiscordRestClient _discord;
        private readonly InteractionService _interactions;
        private readonly string _pbk;
        private readonly IServiceProvider _serviceProvider;
        private readonly RequestDelegate _next;

        public InteractionServiceMiddleware(
            ILogger<InteractionServiceMiddleware> logger,
            DiscordRestClient discordClient,
            InteractionService interactionService,
            string pbk,
            RequestDelegate next,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _discord = discordClient;
            _interactions = interactionService;
            _pbk = pbk;
            _serviceProvider = serviceProvider;
            _next = next;

            _interactions.SlashCommandExecuted += SlashCommandExecuted;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            async Task RespondAsync(int statusCode, string responseBody)
            {
                httpContext.Response.StatusCode = statusCode;
                httpContext.Response.ContentType = "application/json";
                await httpContext.Response.WriteAsync(responseBody).ConfigureAwait(false);
                await httpContext.Response.CompleteAsync().ConfigureAwait(false);
            }

            var signature = httpContext.Request.Headers["X-Signature-Ed25519"];
            var timestamp = httpContext.Request.Headers["X-Signature-Timestamp"];
            using var sr = new StreamReader(httpContext.Request.Body);
            var body = await sr.ReadToEndAsync();

            if (!_discord.IsValidHttpInteraction(_pbk, signature, timestamp, body))
            {
                await RespondAsync(StatusCodes.Status400BadRequest, "Invalid Interaction Signature!");
                return;
            }

            RestInteraction interaction = await _discord.ParseHttpInteractionAsync(_pbk, signature, timestamp, body);

            if (interaction is RestPingInteraction pingInteraction)
            {
                await RespondAsync(StatusCodes.Status200OK, pingInteraction.AcknowledgePing());
                return;
            }

            var interactionCtx = new RestInteractionContext(_discord, interaction, (str) => RespondAsync(StatusCodes.Status200OK, str));
            var result = await _interactions.ExecuteCommandAsync(interactionCtx, _serviceProvider);
        }

        public async Task SlashCommandExecuted(SlashCommandInfo info, IInteractionContext context, DNetInteractions.IResult result)
        {
            if (!result.IsSuccess && result.Error != null)
            {
                var userDisplay = $"{Format.UsernameAndDiscriminator(context.User)} ({context.User.Id})";
                var guildDisplay = $"{context.Guild.Name} ({context.Guild.Id})";
                _logger.LogError(message: "Error running {Command} for {User} in {Guild} - {Error}: {ErrorReason}", info.Name, userDisplay, guildDisplay, result.Error, result.ErrorReason);

                var WarningEmoji = new Emoji("\u26A0");
                var embed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithTitle($"{WarningEmoji} Error!")
                    .WithDescription(result.ErrorReason)
                    .Build();
                try
                {
                    await context.Interaction.RespondAsync(ephemeral: true, embed: embed);
                }
                catch
                {
                    try
                    {
                        await context.Interaction.FollowupAsync(ephemeral: true, embed: embed);
                    }
                    catch { }
                }
            }
            await Task.CompletedTask;
        }
    }
}