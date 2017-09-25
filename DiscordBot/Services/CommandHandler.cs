using System;
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
                var result = await Command.ExecuteAsync(context, argPos, Provider);

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
}