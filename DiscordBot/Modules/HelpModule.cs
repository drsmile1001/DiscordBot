using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.Configuration;

namespace DiscordBot.Modules
{
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        public HelpModule(CommandService service,
            IConfigurationRoot config)
        {
            Service = service;
            Config = config;
        }

        private CommandService Service { get; }

        private IConfigurationRoot Config { get; }

        [Alias("?")]
        [Command("help")]
        public async Task HelpAsync()
        {
            await ReplyAsync("help");
        }

    }
}
