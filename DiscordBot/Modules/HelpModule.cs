using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace DiscordBot.Modules
{
    [Name("說明模組")]
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        public HelpModule(CommandService service,
            IConfigurationRoot config,
            DiscordSocketClient client)
        {
            Service = service;
            Config = config;
            Client = client;
        }

        private CommandService Service { get; }

        private IConfigurationRoot Config { get; }

        private DiscordSocketClient Client { get; }

        [Command("help")]
        [Alias("?")]
        [Summary("列出可用指令")]
        public async Task HelpAsync()
        {
            var prefix = Config["prefix"];
            var builder = new EmbedBuilder
            {
                Color = new Color(114, 137, 218),
                Description = $"命令前綴{prefix}",
                Title = "可用命令："
            };

            foreach (var module in Service.Modules)
            {
                string description = null;
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess)
                        description +=
                            $"{string.Join(", ", cmd.Aliases.OrderByDescending(item => item.Length))} {cmd.Summary}\r\n";
                }

                if (!string.IsNullOrWhiteSpace(description))
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = false;
                    });
            }

            await ReplyAsync("", false, builder.Build());
        }

        
        [Command("help")]
        [Alias("?")]
        [Summary("查詢特定命令")]
        public async Task HelpAsync([Summary("要查詢的命令")] string command)
        {
            var result = Service.Search(Context, command);

            if (!result.IsSuccess)
            {
                await ReplyAsync($"無法找到命令 {command}");
                return;
            }

            var builder = new EmbedBuilder
            {
                Color = new Color(114, 137, 218),
                Title = $"查詢命令 {command}",
                Description = "命令, 替代命令1, 替代命令2.. [參數1] [參數2]..\r\n命令說明"
            };

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;

                builder.AddField(x =>
                {
                    var aliases = string.Join(", ", cmd.Aliases.OrderByDescending(item => item.Length));
                    var parameters = string.Join(" ", cmd.Parameters.Select(p => $"[{p.Summary ?? p.Name}]"));
                    x.Name = $"{aliases} {parameters}";
                    x.Value = cmd.Summary;
                    x.IsInline = false;
                });
            }

            await ReplyAsync("", false, builder.Build());
        }

        [Command("game")]
        [Alias("g")]
        [Summary("設定bot在玩的遊戲")]
        public async Task GameAsync([Summary("遊戲名稱")] string game)
        {
            await Client.SetGameAsync(game);
        }
    }
}