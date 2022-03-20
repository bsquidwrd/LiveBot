using LiveBot.Watcher.Twitch;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.SetupLiveBot();
builder.Services.AddSingleton<TwitchMonitor>();

var app = builder.Build();

app.UseSerilogRequestLogging();

var monitor = app.Services.GetRequiredService<TwitchMonitor>();
await monitor.StartAsync(IsWatcher: true);

app.MapGet("/", () => "Up");

app.Run();