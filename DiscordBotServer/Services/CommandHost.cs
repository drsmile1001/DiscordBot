using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBotServer.Utilities;

namespace DiscordBotServer.Services;

public class CommandHost : IHostedService
{
    private readonly CommandService _commandService;
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordClientHost _clientHost;
    private readonly ILogger _logger;
    private readonly string _commandPrefix;

    public CommandHost(IConfiguration config,
                       IServiceProvider serviceProvider,
                       DiscordClientHost clientHost,
                       ILogger<CommandHost> logger)
    {
        _commandService = new CommandService(new CommandServiceConfig
        {
            DefaultRunMode = RunMode.Async,
            LogLevel = LogSeverity.Info
        });
        _serviceProvider = serviceProvider;
        _clientHost = clientHost;
        _logger = logger;
        _clientHost.Client.MessageReceived += OnMessageReceivedAsync;
        _commandPrefix = config.GetValue<string>("CommandPrefix");
        _commandService.Log += Log;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _clientHost.Client.MessageReceived -= OnMessageReceivedAsync;
        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(SocketMessage socketMessage)
    {
        if (socketMessage is not SocketUserMessage msg) return;
        if (msg.Author.Id == _clientHost.Client.CurrentUser.Id) return;
        if (msg.Author.IsBot) return;

        var context = new SocketCommandContext(_clientHost.Client, msg);
        var argPos = 0;
        if (msg.HasStringPrefix(_commandPrefix, ref argPos) ||
            msg.HasMentionPrefix(_clientHost.Client.CurrentUser, ref argPos))
        {
            await ExecuteCommandAsync(context, context.Message.Content[argPos..]);
            return;
        }
        var command = context.Message.Content.Split('\r', '\n')
            .FirstOrDefault(line => line.StartsWith(_commandPrefix));
        if (command != null)
            await ExecuteCommandAsync(context, command[_commandPrefix.Length..]);
    }

    private async Task ExecuteCommandAsync(SocketCommandContext context, string input)
    {
        var result = await _commandService.ExecuteAsync(context, input, _serviceProvider);
        if (result.IsSuccess) return;
        await context.Channel.SendMessageAsync(result.ErrorReason);
    }

    private Task Log(LogMessage arg)
    {
        _logger.LogDiscordLogMessage(arg);
        return Task.CompletedTask;
    }
}