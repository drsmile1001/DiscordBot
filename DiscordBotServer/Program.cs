using DiscordBotServer.Entities;
using DiscordBotServer.Services;
using DiscordBotServer.Utilities;
using Microsoft.EntityFrameworkCore;
using Serilog;

var logConfiguration = GetLoggerConfiguration(args);
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(logConfiguration)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.ConfigureAppConfiguration(c =>
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var sourceBeforeEnvJson = new Stack<IConfigurationSource>();

        while (true)
        {
            var source = c.Sources[^1];
            if (source is Microsoft.Extensions.Configuration.Json.JsonConfigurationSource jsonSource
                && jsonSource.Path == $"appsettings.{environment}.json") break;
            sourceBeforeEnvJson.Push(source);
            c.Sources.RemoveAt(c.Sources.Count - 1);
        }

        c.AddJsonFile($"appsettings.{environment}.local.json", optional: true, reloadOnChange: true);

        while (sourceBeforeEnvJson.TryPop(out var source))
        {
            c.Add(source);
        }
    });
    builder.Host.UseSerilog();
    builder.Services.AddDbContext<AppDbContext>(config => config.UseSqlite("Data Source=data/discord.db"));
    builder.Services.AddSingletonHostedService<DiscordClientHost>();
    builder.Services.AddHostedService<CommandHost>();
    builder.Services.AddHostedService<PTTPreviewerHost>();
    builder.Services.AddHostedService<FacebookPreviewerHost>();
    builder.Services.AddHostedService<AutoLagHost>();

    var app = builder.Build();
    app.MapGet("/", () => "Discord Bot Server");
    app.MapPost("/api/MessagePresetsBatch", async (MessagePreset[] inputs, AppDbContext context) =>
    {
        context.MessagePreset.AddRange(inputs);
        await context.SaveChangesAsync();
        return Results.Ok();
    });
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static IConfiguration GetLoggerConfiguration(string[] args)
{
    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

    var configurationBuilder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("serilog.json", optional: true, reloadOnChange: true)
        .AddJsonFile($"serilog.{environment}.json", optional: true, reloadOnChange: true);

    configurationBuilder.AddEnvironmentVariables();
    configurationBuilder.AddCommandLine(args);

    return configurationBuilder.Build();
}
