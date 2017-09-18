using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace DiscordBot.Services
{
    internal class StartupService
    {
        public StartupService(DiscordSocketClient discord,
            CommandService command,
            IConfigurationRoot config)
        {
            Config = config;
            Discord = discord;
            Command = command;
        }

        private DiscordSocketClient Discord { get; }

        private CommandService Command { get; }

        private IConfigurationRoot Config { get; }

        public async Task StartAsync()
        {
            var discordToken = Config["tokens:discord"];
            if (string.IsNullOrWhiteSpace(discordToken))
                throw new Exception("需要在appSettings.json的tokens.discord中存放Bot的token");
            await Discord.LoginAsync(TokenType.Bot, discordToken);
            await Discord.StartAsync();
            await Command.AddModulesAsync(Assembly.GetEntryAssembly());
        }
    }
}