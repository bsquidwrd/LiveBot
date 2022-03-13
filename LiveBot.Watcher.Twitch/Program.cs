using LiveBot.Watcher.Twitch;
using Serilog;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) =>
    lc
        .MinimumLevel.Verbose()
        .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(formatter: new JsonFormatter(), path: "logs/log-.json", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 31)
        .Enrich.FromLogContext()
);
builder.SetupLiveBotDatabase();
builder.Services.AddSingleton<TwitchMonitor>();

var app = builder.Build();

app.UseSerilogRequestLogging();

var monitor = app.Services.GetRequiredService<TwitchMonitor>();
monitor.IsWatcher = true;
await monitor.StartAsync();

app.MapGet("/", () => "Up");

app.Run();