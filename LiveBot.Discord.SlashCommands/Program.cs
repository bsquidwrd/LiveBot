using LiveBot.Discord.SlashCommands;

var builder = WebApplication.CreateBuilder(args);

await builder.SetupLiveBot();

var app = builder.Build();

await app.RegisterLiveBot();

app.MapGet("/", () => "Up");

app.Run();