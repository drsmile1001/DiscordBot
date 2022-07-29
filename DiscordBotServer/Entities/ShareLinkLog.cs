namespace DiscordBotServer.Entities;

public class ShareLinkLog
{
    public ulong Id { get; set; }
    public string Link { get; set; } = null!;
    public ulong SenderId { get; set; } = 0;
    public long CreatedTimestamp { get; set; }
}