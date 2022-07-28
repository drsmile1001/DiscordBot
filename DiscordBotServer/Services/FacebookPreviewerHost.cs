using Discord;
using Discord.WebSocket;
using HtmlAgilityPack;

namespace DiscordBotServer.Services;

public class FacebookPreviewerHost : IHostedService
{
    private readonly DiscordClientHost _clientHost;

    public FacebookPreviewerHost(DiscordClientHost clientHost)
    {
        _clientHost = clientHost;
        _clientHost.Client.MessageReceived += OnMessageReceivedAsync;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _clientHost.Client.MessageReceived -= OnMessageReceivedAsync;
        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(SocketMessage socketMessage)
    {
        if (socketMessage is not SocketUserMessage msg) return;
        if (msg.Author.Id == _clientHost.Client.CurrentUser.Id) return;
        if (msg.Author.IsBot) return;

        if (!msg.Content.StartsWith("https://www.facebook.com")) return;

        var web = new HtmlWeb();
        var contentDoc = web.Load(msg.Content);

        var description = contentDoc.DocumentNode.Descendants("meta").First(m => m.Attributes["name"]?.Value == "description")
            .Attributes["content"].Value;

        var title = contentDoc.DocumentNode.Descendants("meta").First(m => m.Attributes["property"]?.Value == "og:title")
            .Attributes["content"].Value;

        await msg.ReplyAsync(embed: new EmbedBuilder()
            .WithTitle(HtmlEntity.DeEntitize(title))
            .WithDescription(HtmlEntity.DeEntitize(description))
            .WithUrl(msg.Content)
            .Build(), allowedMentions: AllowedMentions.None);

        await msg.ModifyAsync(p =>
        {
            p.Flags = MessageFlags.SuppressEmbeds;
        });
    }
}