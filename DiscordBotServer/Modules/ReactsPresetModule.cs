using Discord;
using Discord.Commands;
using DiscordBotServer.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBotServer.Modules
{
    [Name("反應模組")]
    public class ReactsPresetModule : ModuleBase<SocketCommandContext>
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ReactsPresetModule(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        [Command("react")]
        [Summary("對特定訊息實行一系列反應")]
        public async Task React([Summary("反應ID")] string reactId, [Summary("訊息ID")] ulong id)
        {
            using var scope = _scopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var reactsPreset = db.ReactsPreset.FirstOrDefault(item => item.Id == reactId);
            if (reactsPreset == null)
            {
                await ReplyAsync("找不到反應預設集");
                return;
            }

            var message = await Context.Channel.GetMessageAsync(id);
            if(message == null)
            {
                await ReplyAsync("找不到訊息");
                return;
            }
            var userMessage = message as IUserMessage;
            var reactions = reactsPreset.Reactions.Split(',').ToArray();
            foreach (var reaction in reactions)
            {
                await userMessage.AddReactionAsync(new Emoji(reaction));
            }
        }

        [Command("react-set")]
        [Summary("設定反應預設集")]
        public async Task React([Summary("反應ID")] string reactId,[Summary("反應集")] string reactions)
        {
            using var scope = _scopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var oldPreset = db.ReactsPreset.SingleOrDefault(preset => preset.Id == reactId);
            if (oldPreset == null)
            {
                var newReactsPreset = new ReactsPreset
                {
                    Id = reactId,
                    Reactions = reactions
                };
                db.ReactsPreset.Add(newReactsPreset);
            }
            else
                oldPreset.Reactions = reactions;
            db.SaveChanges();
            await ReplyAsync("已設定反應預設集");
        }
    }
}
