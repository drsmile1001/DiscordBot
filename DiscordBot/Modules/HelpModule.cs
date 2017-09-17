using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.Configuration;

namespace DiscordBot.Modules
{
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        public HelpModule(CommandService service,
            IConfigurationRoot config,
            Random random)
        {
            Service = service;
            Config = config;
            Random = random;
        }

        private CommandService Service { get; }

        private IConfigurationRoot Config { get; }

        private Random Random { get; }

        [Alias("?")]
        [Command("help")]
        public async Task HelpAsync()
        {
            await ReplyAsync($"{Random.NextDouble()}");
        }
    }
}
