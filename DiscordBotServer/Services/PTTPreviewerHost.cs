using Discord;
using Discord.WebSocket;
using Flurl.Http;
using HtmlAgilityPack;

namespace DiscordBotServer.Services;

public class PTTPreviewerHost : IHostedService
{
    private readonly DiscordClientHost _clientHost;
    private readonly ILogger _logger;

    public PTTPreviewerHost(DiscordClientHost clientHost,
                       ILogger<PTTPreviewerHost> logger)
    {
        _clientHost = clientHost;
        _logger = logger;
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

        if (!msg.Content.StartsWith("https://www.ptt.cc/bbs")) return;

        var checkHtml = await msg.Content
            .GetStringAsync();

        var checkDoc = new HtmlDocument();
        checkDoc.LoadHtml(checkHtml);

        if (checkDoc.DocumentNode.Descendants("div").All(m => m.Attributes["class"]?.Value != "over18-notice")) return;

        var contentHtml = await msg.Content
            .WithCookie("over18", "1")
            .GetStringAsync();

        var contentDoc = new HtmlDocument();
        contentDoc.LoadHtml(contentHtml);

        var description = contentDoc.DocumentNode.Descendants("meta").First(m => m.Attributes["name"]?.Value == "description")
            .Attributes["content"].Value;

        var title = contentDoc.DocumentNode.Descendants("meta").First(m => m.Attributes["property"]?.Value == "og:title")
            .Attributes["content"].Value;

        await msg.Channel.SendMessageAsync(embed: new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithUrl(msg.Content)
            .Build());
    }
}