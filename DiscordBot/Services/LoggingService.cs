using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DiscordBot.Services
{
    internal class LoggingService
    {
        public LoggingService(DiscordSocketClient discord, CommandService command)
        {
            LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            Discord = discord;
            Command = command;
            Discord.Log += DiscordOnLogAsync;
            Command.Log += DiscordOnLogAsync;
        }

        private DiscordSocketClient Discord { get; }

        private CommandService Command { get; }

        private string LogDirectory { get; }

        private string LogFile => Path.Combine(LogDirectory, $"{DateTime.Now:yyyy-MM-dd}.txt");

        private Task DiscordOnLogAsync(LogMessage logMessage)
        {
            var logText =
                $"{DateTime.Now:HH:mm:ss} [{logMessage.Severity}] {logMessage.Source}: {logMessage.Exception?.ToString() ?? logMessage.Message}";
            return LogMessageAsync(logText);
        }

        private Task LogMessageAsync(string logMessage)
        {
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);
            if (!File.Exists(LogFile))
                File.Create(LogFile).Dispose();
            lock (this)
            {
                File.AppendAllText(LogFile, logMessage + "\r\n");
            }
            return Console.Out.WriteLineAsync(logMessage);
        }
    }
}