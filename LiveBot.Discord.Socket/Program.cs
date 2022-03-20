using LiveBot.Discord.Socket;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.SetupLiveBot();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.MapGet("/", () => "Up");

app.Run();