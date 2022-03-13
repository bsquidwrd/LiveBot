using LiveBot.Discord.Socket;
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

builder.SetupLiveBot();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.MapGet("/", () => "Up");

app.Run();