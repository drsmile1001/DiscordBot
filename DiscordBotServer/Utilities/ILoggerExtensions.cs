using Discord;

namespace DiscordBotServer.Utilities;

public static class ILoggerExtensions
{
    public static void LogDiscordLogMessage(this ILogger logger, LogMessage message)
    {
        var level = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Debug => LogLevel.Debug,
            LogSeverity.Verbose => LogLevel.Trace,
            _ => LogLevel.None
        };
        logger.Log(level, message.Exception, "Discord Log: {message}", message.Message);
    }
}