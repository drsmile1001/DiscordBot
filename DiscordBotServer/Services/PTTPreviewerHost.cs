using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Flurl.Http;
using HtmlAgilityPack;

namespace DiscordBotServer.Services;

public class PTTPreviewerHost : IHostedService
{
    private readonly DiscordClientHost _clientHost;

    private readonly Regex _linkRegex = new(@"https://www.ptt.cc/bbs/\S+");

    public PTTPreviewerHost(DiscordClientHost clientHost)
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

        var match = _linkRegex.Match(msg.Content);
        if (!match.Success) return;

        var checkHtml = await match.Value
            .GetStringAsync();

        var checkDoc = new HtmlDocument();
        checkDoc.LoadHtml(checkHtml);

        if (checkDoc.DocumentNode.Descendants("div").All(m => m.Attributes["class"]?.Value != "over18-notice")) return;

        var contentHtml = await match.Value
            .WithCookie("over18", "1")
            .GetStringAsync();

        var contentDoc = new HtmlDocument();
        contentDoc.LoadHtml(contentHtml);

        var description = contentDoc.DocumentNode.Descendants("meta").First(m => m.Attributes["name"]?.Value == "description")
            .Attributes["content"].Value;

        var title = contentDoc.DocumentNode.Descendants("meta").First(m => m.Attributes["property"]?.Value == "og:title")
            .Attributes["content"].Value;

        await msg.ReplyAsync(embed: new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithUrl(match.Value)
            .Build(), allowedMentions: AllowedMentions.None);
    }
}