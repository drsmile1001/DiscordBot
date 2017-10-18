using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace DiscordBot.Services
{
    internal class CommandHandler
    {
        public CommandHandler(DiscordSocketClient discord,
            CommandService command,
            IConfigurationRoot config,
            IServiceProvider provider)
        {
            Config = config;
            Discord = discord;
            Command = command;
            Provider = provider;
            discord.MessageReceived += OnMessageReceivedAsync;
        }

        private IConfigurationRoot Config { get; }
        private DiscordSocketClient Discord { get; }

        private CommandService Command { get; }

        private IServiceProvider Provider { get; }

        private async Task OnMessageReceivedAsync(SocketMessage socketMessage)
        {
            if (!(socketMessage is SocketUserMessage msg)) return;
            if (msg.Author.Id == Discord.CurrentUser.Id) return;
            if (msg.Author.IsBot) return;

            var context = new SocketCommandContext(Discord, msg);
            var argPos = 0;
            if (msg.HasStringPrefix(Config["prefix"], ref argPos) ||
                msg.HasMentionPrefix(Discord.CurrentUser, ref argPos))
            {
                await ExecuteCommand(context, context.Message.Content.Substring(argPos));
                return;
            }
            var command = context.Message.Content.Split('\r', '\n')
                .FirstOrDefault(line => line.StartsWith(Config["prefix"]));
            if (command != null)
                await ExecuteCommand(context, command.Substring(Config["prefix"].Length));
        }

        /// <summary>
        /// 執行命令
        /// </summary>
        /// <param name="context"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        private async Task ExecuteCommand(SocketCommandContext context, string input)
        {
            var result = await Command.ExecuteAsync(context, input, Provider);
            if (!result.IsSuccess)
            {
                switch (result.Error)
                {
                    case CommandError.UnknownCommand:
                        return;
                    case CommandError.ParseFailed:
                    case CommandError.BadArgCount:
                        break;
                    case CommandError.ObjectNotFound:
                    case CommandError.MultipleMatches:
                    case CommandError.UnmetPrecondition:
                    case CommandError.Exception:
                    case CommandError.Unsuccessful:
                    case null:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                await context.Channel.SendMessageAsync(result.ToString());
            }
        }
    }
}