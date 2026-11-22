using LiveBot.Discord.SlashCommands;
using LiveBot.Discord.SlashCommands.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

await builder.SetupLiveBot();

var app = builder.Build();

app.UseSerilogRequestLogging();

await app.RegisterLiveBot();

app.MapGet("/", () => "Up");
app.MapLiveBotHealthChecks();

app.Run();