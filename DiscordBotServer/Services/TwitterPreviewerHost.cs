using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;

namespace DiscordBotServer.Services;

public class TwitterPreviewerHost : IHostedService
{
    private readonly DiscordClientHost _clientHost;
    private readonly Regex _linkRegex = new(@"https://twitter.com\S+");

    public TwitterPreviewerHost(DiscordClientHost clientHost)
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
        var vxurl = msg.Content.Replace("https://twitter.com", "https://vxtwitter.com");

        await msg.ReplyAsync(vxurl);
    }
}