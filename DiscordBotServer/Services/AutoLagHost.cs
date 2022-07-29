using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using DiscordBotServer.Entities;

namespace DiscordBotServer.Services;

public class AutoLagHost : IHostedService
{
    private readonly DiscordClientHost _clientHost;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Regex _httpRegex = new(@"(http|https)://\S+");

    public AutoLagHost(DiscordClientHost clientHost, IServiceScopeFactory scopeFactory)
    {
        _clientHost = clientHost;
        _scopeFactory = scopeFactory;
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

        var content = msg.Content;
        var matches = _httpRegex.Matches(content);
        var timeBound = DateTimeOffset.Now.AddDays(-7).ToUnixTimeMilliseconds();

        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (matches.Any(m => db.ShareLinkLog.Any(log => log.Link == m.Value && log.CreatedTimestamp >= timeBound)))
        {
            await msg.AddReactionAsync(new Emoji("🍗"));
        }

        var logs = matches.Select(m => new ShareLinkLog
        {
            Id = msg.Id,
            Link = m.Value,
            SenderId = msg.Author.Id,
            CreatedTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
        }).ToArray();

        db.ShareLinkLog.AddRange(logs);
        await db.SaveChangesAsync();
    }
}