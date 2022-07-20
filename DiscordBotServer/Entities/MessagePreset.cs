namespace DiscordBotServer.Entities;

public class MessagePreset
{
    /// <summary>
    /// ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 索引
    /// </summary>
    public string Index { get; set; } = string.Empty;

    /// <summary>
    /// 重複索引的流水號
    /// </summary>
    public int SeriesNumber { get; set; }

    /// <summary>
    /// 儲存的字串
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 使用次數
    /// </summary>
    public int CalledCount { get; set; }

    /// <summary>
    /// 最後使用時間
    /// </summary>
    public DateTimeOffset? LastCalledTime { get; set; }

    public string LastCalledTimeText => LastCalledTime?.ToString("u") ?? "尚未使用";

    /// <summary>
    /// 最後的使用者
    /// </summary>
    public ulong? LastCallerId { get; set; }

    /// <summary>
    /// 新增的使用者
    /// </summary>
    public ulong? CreatorId { get; set; }

    public string CreatorText => CreatorId.HasValue ? $"<@{CreatorId}>" : "不明";

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTimeOffset? CreatedTime { get; set; }

    public string CreatedTimeText => CreatedTime?.ToString("u") ?? "不明";
}