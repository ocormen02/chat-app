using ChatApp.Application.Interfaces;
using ChatApp.Bot.Services;
using ChatApp.Bot.Workers;
using ChatApp.Infrastructure.Messaging;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

// Add HttpClient for StockService
builder.Services.AddHttpClient<IStockService, StockService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register RabbitMQ Service
builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();

// Register StockBotWorker
builder.Services.AddHostedService<StockBotWorker>();

var host = builder.Build();

Log.Information("StockBot service starting...");

host.Run();
