using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBotServer.Services
{
    internal class CommandHandler : IHostedService
    {
        private readonly DiscordSocketClient _discordSocketClient;
        private readonly CommandService _commandService;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _commandPrefix;

        public CommandHandler(DiscordSocketClient discordSocketClient,
            CommandService commandService,
            IConfiguration config,
            IServiceProvider serviceProvider)
        {
            
            _discordSocketClient = discordSocketClient;
            _commandService = commandService;
            _serviceProvider = serviceProvider;
            _commandPrefix = config.GetValue<string>("CommandPrefix");
        }

        private async Task OnMessageReceivedAsync(SocketMessage socketMessage)
        {
            if (!(socketMessage is SocketUserMessage msg)) return;
            if (msg.Author.Id == _discordSocketClient.CurrentUser.Id) return;
            if (msg.Author.IsBot) return;

            var context = new SocketCommandContext(_discordSocketClient, msg);
            var argPos = 0;
            if (msg.HasStringPrefix(_commandPrefix, ref argPos) ||
                msg.HasMentionPrefix(_discordSocketClient.CurrentUser, ref argPos))
            {
                await ExecuteCommand(context, context.Message.Content.Substring(argPos));
                return;
            }
            var command = context.Message.Content.Split('\r', '\n')
                .FirstOrDefault(line => line.StartsWith(_commandPrefix));
            if (command != null)
                await ExecuteCommand(context, command.Substring(_commandPrefix.Length));
        }

        /// <summary>
        /// 執行命令
        /// </summary>
        /// <param name="context"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        private async Task ExecuteCommand(SocketCommandContext context, string input)
        {
            var result = await _commandService.ExecuteAsync(context, input, _serviceProvider);
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

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _discordSocketClient.MessageReceived += OnMessageReceivedAsync;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}