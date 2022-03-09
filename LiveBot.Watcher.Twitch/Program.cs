using LiveBot.Watcher.Twitch;

var builder = WebApplication.CreateBuilder(args);
builder.SetupLiveBotDatabase();
builder.Services.AddSingleton<TwitchMonitor>();

var app = builder.Build();

var monitor = app.Services.GetRequiredService<TwitchMonitor>();
monitor.IsWatcher = true;
await monitor.StartAsync();

app.MapGet("/", () => "Up");

app.Run();