using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Model;
using DiscordBot.Services;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FileMode = System.IO.FileMode;

namespace DiscordBot
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        {
            var services = new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Info,
                    MessageCacheSize = 1000
                }))
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    DefaultRunMode = RunMode.Async,
                    LogLevel = LogSeverity.Info
                }))
                .AddSingleton<CommandHandler>()
                .AddSingleton<LoggingService>()
                .AddSingleton<StartupService>()
                .AddSingleton(new Random(Guid.NewGuid().GetHashCode()))
                .AddSingleton(new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appSettings.json", true, true)
                    .Build())
                .AddSingleton(serviceProvider =>
                {
                    var fileName = Path.Combine(AppContext.BaseDirectory, "discordBotDb.db");
                    var fileStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                        FileShare.Read);
                    var db = new DiscordBotDb(fileStream);
                    return db;
                });

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<LoggingService>();
            await provider.GetRequiredService<StartupService>().StartAsync();
            provider.GetRequiredService<CommandHandler>();

            await Task.Delay(-1);
        }

    }
}
