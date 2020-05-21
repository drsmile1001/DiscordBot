using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBotServer.Services
{
    internal class DiscordLoggingService : IHostedService
    {
        private readonly DiscordSocketClient _discordSocketClient;

        private readonly CommandService _commandService;

        private readonly ILogger<DiscordLoggingService> _logger;

        public DiscordLoggingService(DiscordSocketClient discordSocketClient, CommandService commandService, ILogger<DiscordLoggingService> logger)
        {
            _discordSocketClient = discordSocketClient;
            _commandService = commandService;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _discordSocketClient.Log += DiscordOnLogAsync;
            _commandService.Log += DiscordOnLogAsync;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private Task DiscordOnLogAsync(LogMessage logMessage)
        {
            var logLevel = LogLevel.Critical - (int)logMessage.Severity;
            if(logMessage.Exception != null)
                _logger.Log(logLevel, logMessage.Exception, logMessage.Message);
            else
                _logger.Log(logLevel, logMessage.Message);
            return Task.CompletedTask;
        }
    }
}