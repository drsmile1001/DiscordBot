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
            Discord.Log += OnLogAsunc;
            Command.Log += OnLogAsunc;
        }

        private DiscordSocketClient Discord { get; }

        private CommandService Command { get; }

        private string LogDirectory { get; }

        private string LogFile => Path.Combine(LogDirectory, $"{DateTime.Now:yyyy-MM-DD}.text");

        private Task OnLogAsunc(LogMessage logMessage)
        {
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);
            if (!File.Exists(LogFile))
                File.Create(LogFile).Dispose();
            var logText =
                $"{DateTime.Now:hh:mm:ss} [{logMessage.Severity}] {logMessage.Source}: {logMessage.Exception?.ToString() ?? logMessage.Message}";
            lock (this)
            {
                File.AppendAllText(LogFile, logText + "\r\n");
            }
            return Console.Out.WriteLineAsync(logText);
        }
    }
}