using LiveBot.Discord.SlashCommands;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

await builder.SetupLiveBot();

var app = builder.Build();

app.UseSerilogRequestLogging();

await app.RegisterLiveBot();

app.MapGet("/", () => "Up");
app.MapGet("/healthcheck", () => {
  Console.WriteLine("Healthcheck Success");
  return "OK";
});

app.Run();
