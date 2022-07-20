using Discord;
using Discord.WebSocket;
using DiscordBotServer.Utilities;

namespace DiscordBotServer.Services;

public class DiscordClientHost : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly string _token;
    private readonly ILogger logger;

    public DiscordClientHost(IConfiguration configuration, ILogger<DiscordClientHost> logger)
    {
        _client = new(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info,
            MessageCacheSize = 1000
        });
        _client.Log += Log;
        _token = configuration.GetValue<string>("DiscordToken");
        this.logger = logger;
    }

    public DiscordSocketClient Client => _client;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
    }

    private Task Log(LogMessage arg)
    {
        logger.LogDiscordLogMessage(arg);
        return Task.CompletedTask;
    }
}