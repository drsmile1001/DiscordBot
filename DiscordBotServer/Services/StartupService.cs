using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBotServer.Services
{
    internal class StartupService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DiscordSocketClient _discordSocketClient;
        private readonly CommandService _commandService;
        private readonly string _discordToken;

        public StartupService(DiscordSocketClient discordSocketClient,
            CommandService commandService,
            IConfiguration config,
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _discordSocketClient = discordSocketClient;
            _commandService = commandService;
            _discordToken = config.GetValue<string>("DiscordToken");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_discordToken))
                throw new Exception("需要在appSettings.json的tokens.discord中存放Bot的token");
            await _discordSocketClient.LoginAsync(TokenType.Bot, _discordToken);
            await _discordSocketClient.StartAsync();
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
            _discordSocketClient.Disconnected += Disconnected;

        }

        private Task Disconnected(Exception arg)
        {
            return _discordSocketClient.StartAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}