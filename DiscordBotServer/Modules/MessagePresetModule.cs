using System.Runtime.InteropServices;
using Discord;
using Discord.Commands;
using DiscordBotServer.Entities;
using F23.StringSimilarity;

namespace DiscordBotServer.Modules;

[Name("預設訊息")]
public class MessagePresetModule : ModuleBase<SocketCommandContext>
{
    /// <summary>
    ///     內嵌內容一頁要顯示的筆數
    /// </summary>
    private const int _tabLength = 10;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Random _random = new(Guid.NewGuid().GetHashCode());

    private readonly JaroWinkler _jw = new();

    /// <summary>
    ///     相似度門檻
    /// </summary>
    private const double _similarityThreshold = 0.6;

    public MessagePresetModule(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    ///     找到符合索引或接近索引的集合
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private (MessagePreset[] founds, double? similarity) FoundPresetText(string index)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var founds = db.MessagePreset
            .Where(text => text.Index == index)
            .ToArray();
        if (founds.Length != 0)
            return (founds, null);

        var regularedIndex = Path.GetFileNameWithoutExtension(index);
        var extension = Path.GetExtension(index);
        var reverseIndex = new string(regularedIndex.Reverse().ToArray());

        var foundsBySmilar = db.MessagePreset
            .ToArray()
            .GroupBy(item => item.Index)
            .AsParallel()
            .Select(indexGrouping =>
            {
                var sampleIndex = indexGrouping.Key;

                var regularedSampleIndex = Path.GetFileNameWithoutExtension(sampleIndex);
                var sampleExtension = Path.GetExtension(sampleIndex);
                var reverseSampleIndex = new string(regularedSampleIndex.Reverse().ToArray());

                var similarity = _jw.Similarity(regularedIndex, regularedSampleIndex);
                var extensionSimilarity = _jw.Similarity(extension, sampleExtension);
                var reverseSimilarity = _jw.Similarity(reverseIndex, reverseSampleIndex);

                return new
                {
                    Similarity = similarity * 0.49 + reverseSimilarity * 0.49 + extensionSimilarity * 0.02,
                    IndexGrouping = indexGrouping
                };
            })
            .OrderByDescending(item => item.Similarity)
            .FirstOrDefault();

        return (foundsBySmilar?.IndexGrouping.ToArray() ?? Array.Empty<MessagePreset>(), foundsBySmilar?.Similarity);
    }

    /// <summary>
    ///     回應預設字串
    /// </summary>
    /// <param name="found"></param>
    /// <param name="foundSimilarity"></param>
    /// <returns></returns>
    private Task ReplyPresetText(MessagePreset found, double? foundSimilarity)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            found.CalledCount += 1;
            found.LastCalledTime = DateTimeOffset.UtcNow;
            found.LastCallerId = Context.User.Id;
            db.Update(found);
            db.SaveChanges();
        }
        catch (Exception e)
        {
            return ReplyToCommand(e.Message);
        }

        return ReplyToCommand(foundSimilarity != null
            ? $"{found.Index} ({foundSimilarity:P}) #{found.SeriesNumber} {found.Text}"
            : $"{found.Index} #{found.SeriesNumber} {found.Text}");
    }

    private async Task ReplyToCommand(string? message = null, Embed? embed = null)
    {
        await Context.Message.ReplyAsync(message,
                                         embed: embed,
                                         allowedMentions: AllowedMentions.None);
    }

    [Command("presetText")]
    [Alias("+")]
    [Summary("輸出儲存過的字串")]
    public Task PresetText([Summary("索引")] string index, [Summary("流水號")][Optional] string subIndex)
    {
        var userId = Context.User.Id;
        var (founds, similarity) = FoundPresetText(index);
        if (founds.Length == 0)
            return ReplyToCommand("沒有預存字串，加點水吧:sweat_drops:");
        if (similarity < _similarityThreshold)
            return ReplyToCommand("找不到接近的 怕:confounded:");

        MessagePreset? found;

        //依據subindex再縮限
        if (subIndex != null)
        {
            if (!int.TryParse(subIndex, out var subIndexInt))
                return ReplyToCommand("流水號必須為整數");

            found = founds.FirstOrDefault(text => text.SeriesNumber == subIndexInt);
            if (found == null)
                return ReplyToCommand(similarity != null
                    ? $"找不到 {founds.First().Index} ({similarity:P}) #{subIndex}"
                    : $"找不到 {founds.First().Index} #{subIndex}");

            return ReplyPresetText(found, similarity);
        }

        //嘗試找到自己設定中最晚使用的
        found = founds.Where(text => text.CreatorId == userId)
                    .OrderByDescending(text => text.LastCalledTime)
                    .FirstOrDefault()
                //再嘗試找到所有人中最晚使用的
                ?? founds.OrderByDescending(text => text.LastCalledTime)
                    .First();
        return ReplyPresetText(found, similarity);
    }

    [Command("addPresetText")]
    [Alias("++")]
    [Summary("新增字串")]
    public Task AddPresetText([Summary("索引")] string index, [Summary("要儲存的字串")][Remainder] string text)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pool = db.MessagePreset
            .Where(presetText => presetText.Index == index)
            .ToList();
        var found = pool
            .Where(presetText => presetText.Text == text)
            .OrderByDescending(presetText => presetText.LastCalledTime)
            .FirstOrDefault();

        if (found != null)
            return ReplyToCommand($"已存在預存字串 {index} #{found.SeriesNumber}");

        var lastSubIndex = pool.Select(item => item.SeriesNumber).DefaultIfEmpty(-1).Max() + 1;
        try
        {
            db.MessagePreset.Add(new MessagePreset
            {
                Id = Guid.NewGuid().ToString(),
                Index = index,
                SeriesNumber = lastSubIndex,
                Text = text,
                CalledCount = 0,
                CreatorId = Context.User.Id,
                CreatedTime = DateTimeOffset.UtcNow
            });
            db.SaveChanges();
            var builder = new EmbedBuilder
            {
                Title = $"已新增預存字串 {index} #{lastSubIndex}",
                Description = text
            };
            return ReplyToCommand(string.Empty, builder.Build());
        }
        catch (Exception ex)
        {
            return ReplyToCommand($"新增時發生錯誤：{ex.Message}");
        }
    }

    /// <summary>
    ///     分頁範圍字串
    /// </summary>
    /// <param name="tabInt"></param>
    /// <returns></returns>
    private static string TabRangeDescription(int tabInt)
    {
        return $"顯示{(tabInt - 1) * _tabLength + 1}-{(tabInt - 1) * _tabLength + _tabLength}";
    }

    [Command("detailPresetText")]
    [Alias("+?")]
    [Summary("查詢預存字串狀態，流水號可填寫\"_\"以查找字串池的其他分頁")]
    public Task DetailPresetText([Summary("索引")] string index, [Summary("流水號")][Optional] string subIndex,
        [Summary("分頁")][Optional] string tab)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (subIndex == null || subIndex == "_")
        {
            var targets = db.MessagePreset
                .AsQueryable()
                .Where(text => text.Index == index)
                .OrderBy(item => item.SeriesNumber)
                .ToList();
            if (targets.Count == 0)
                return ReplyToCommand($"沒有預存字串的索引為{index}");
            if (!int.TryParse(tab, out var tabInt))
                tabInt = 1;

            var builder = new EmbedBuilder
            {
                Title = index,
                Description = $"共{targets.Count}筆",
                Footer = new EmbedFooterBuilder().WithText(TabRangeDescription(tabInt))
            };

            foreach (var presetText in targets.Skip((tabInt - 1) * _tabLength).Take(_tabLength))
            {
                builder.AddField(x =>
                {
                    x.Name = $"#{presetText.SeriesNumber}";
                    x.Value = $"字串:\"{presetText.Text}\"\n" +
                            $"使用次數:{presetText.CalledCount}\n" +
                            $"最後使用時間:{presetText.LastCalledTimeText}\n" +
                            $"新增人:{presetText.CreatorText}\n" +
                            $"新增時間:{presetText.CreatedTimeText}";
                    x.IsInline = false;
                });
            }
            return ReplyToCommand(string.Empty, builder.Build());
        }
        if (!int.TryParse(subIndex, out var subIndexInt))
            return ReplyToCommand("流水號應為整數");

        var found = db.MessagePreset.FirstOrDefault(text => text.Index == index && text.SeriesNumber == subIndexInt);
        if (found == null)
            return ReplyToCommand($"找不到 {index} #{subIndex}");
        var singleBuilder = new EmbedBuilder
        {
            Title = $"{index} #{subIndex}",
            Description = found.Text
        }.AddField(x =>
        {
            x.Name = found.Text;
            x.Value = $"使用次數:{found.CalledCount}\n" +
                      $"最後使用時間:{found.LastCalledTimeText}\n" +
                      $"新增人:{found.CreatorText}\n" +
                      $"新增時間:{found.CreatedTimeText}";
            x.IsInline = false;
        });
        return ReplyToCommand(string.Empty, singleBuilder.Build());
    }

    [Command("editPresetText")]
    [Alias("+=")]
    [Summary("編輯預存字串索引")]
    public Task EditPresetText([Summary("索引")] string index, [Summary("流水號")] string subIndex,
        [Summary("新索引")] string newIndex)
    {
        if (!int.TryParse(subIndex, out var subIndexInt))
            return ReplyToCommand("流水號應為整數");
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var found = db.MessagePreset.FirstOrDefault(text => text.Index == index && text.SeriesNumber == subIndexInt);
        if (found == null)
            return ReplyToCommand($"找不到 {index} #{subIndex}");

        var newSeriesNumber = db.MessagePreset.AsQueryable()
            .Where(presetText => presetText.Index == newIndex)
            .Select(preset => preset.SeriesNumber)
            .DefaultIfEmpty(-1)
            .Max() + 1;
        found.Index = newIndex;
        found.SeriesNumber = newSeriesNumber;
        db.Update(found);
        db.SaveChanges();
        return ReplyToCommand($"已更新[{index} #{subIndex}]為[{newIndex} #{newSeriesNumber}]");
    }

    [Command("randomPresetText")]
    [Alias("+*")]
    [Summary("亂數輸出儲存過的字串")]
    public Task RandomPresetText([Summary("索引")][Remainder] string index)
    {
        var (founds, similarity) = FoundPresetText(index);
        if (founds.Length == 0)
            return ReplyToCommand("沒有預存字串，加點水吧:sweat_drops:");
        if (similarity < _similarityThreshold)
            return ReplyToCommand("找不到接近的 怕:confounded:");

        var found = founds.AsParallel()
            .GroupBy(item => item.Text)
            .Select(grouping => grouping.OrderByDescending(item => item.LastCalledTime).First())
            .OrderByDescending(item => item.LastCalledTime)
            .Select((item, i) => new
            {
                Weights = (i * 0.9 / founds.Length + 0.1) * _random.NextDouble(),
                PresetText = item
            })
            .OrderByDescending(item => item.Weights)
            .First().PresetText;
        return ReplyPresetText(found, similarity);
    }

    [Command("listPresetText")]
    [Alias("+.")]
    [Summary("列出預存字串")]
    public Task ListPresetText([Summary("分頁")][Optional] string tab, [Optional] string index)
    {
        if (!int.TryParse(tab, out var tabInt))
            tabInt = 1;
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var allPresetText = db.MessagePreset.ToList();
        if (!string.IsNullOrWhiteSpace(index))
            allPresetText = allPresetText.Where(preset => preset.Index.Contains(index)).ToList();

        var source = allPresetText.AsParallel()
            .GroupBy(item => item.Text)
            .Select(
                grouping =>
                {
                    var title = grouping.OrderByDescending(item => item.CalledCount).First();
                    var useCount = grouping.Sum(item => item.CalledCount);
                    return new
                    {
                        Description = $"{title.Index} #{title.SeriesNumber}",
                        UseCount = useCount,
                        title.Text,
                        Grouping = grouping.Select(item => new
                        {
                            item.Index,
                            item.SeriesNumber
                        }).ToList()
                    };
                }).OrderByDescending(item => item.UseCount).ToList();

        var builder = new EmbedBuilder
        {
            Description = $"可用的預存字串總數:{source.Count} {TabRangeDescription(tabInt)}"
        };

        foreach (var group in source.Skip((tabInt - 1) * _tabLength).Take(_tabLength))
            builder.AddField(x =>
            {
                x.Name = group.Description;
                var meta = $"使用次數:{group.UseCount}\n"
                           + group.Grouping.Select(item => $"{item.Index} #{item.SeriesNumber}")
                               .Aggregate((agg, next) => $"{agg}\n{next}");
                var allowLength = 1024 - meta.Length - 20;
                var showText = group.Text.Length >= allowLength
                    ? $"{group.Text[..allowLength]}..."
                    : group.Text;
                x.Value = $"字串:{showText}\n" + meta;
                x.IsInline = false;
            });
        return ReplyToCommand(string.Empty, builder.Build());
    }

    [Command("deletePresetText")]
    [Alias("+-")]
    [Summary("刪除預存字串")]
    public Task DeletePresetText([Summary("索引")] string index, [Summary("流水號")] string subIndex)
    {
        if (!int.TryParse(subIndex, out var subIndexInt))
            return ReplyToCommand("必須輸入整數流水號");
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var found = db.MessagePreset.AsQueryable()
            .Where(item => item.Index == index && item.SeriesNumber == subIndexInt)
            .FirstOrDefault();
        if (found == null)
            return ReplyToCommand($"找不到 {index} #{subIndex}");
        try
        {
            db.MessagePreset.Remove(found);
            db.SaveChanges();
            return ReplyToCommand($"已刪除 {index} #{subIndex}");
        }
        catch (Exception e)
        {
            return ReplyToCommand(e.Message);
        }
    }

    [Command("analyzePresetText")]
    [Alias("+.?")]
    [Summary("分析預存字串")]
    public Task AnalyzePresetText()
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var allPresetText = db.MessagePreset.ToList();
        var toTextCount = allPresetText.Select(text => text.Text).Distinct().Count();
        var builder = new EmbedBuilder
        {
            Title = $"預存字串共{allPresetText.Count}筆索引，指向{toTextCount}筆字串"
        };
        var creatorGroupingBy = allPresetText.GroupBy(text => text.CreatorText).Select(grouping => new
        {
            Creator = grouping.Key,
            Count = grouping.Count()
        })
            .OrderByDescending(item => item.Count)
            .ToList();
        foreach (var item in creatorGroupingBy)
            builder.AddField(item.Creator, $"{item.Count}筆", true);
        return ReplyToCommand(string.Empty, builder.Build());
    }

    [Command("analyzePresetText")]
    [Alias("+.?")]
    [Summary("分析預存字串(建立人)")]
    public Task AnalyzePresetText([Summary("建立人ID")] ulong creatorId)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var presetTexts = db.MessagePreset.AsQueryable()
            .Where(text => text.CreatorId == creatorId)
            .ToList();
        if (presetTexts.Count == 0)
            return ReplyToCommand("查無此使用者或此使用者未建立任何預存字串");
        var toTextCount = presetTexts.Select(text => text.Text).Distinct().Count();
        var description = string.Join(',', presetTexts.OrderByDescending(text => text.CalledCount)
            .Select(text => $"{text.Index}#{text.SeriesNumber}"));
        var builder = new EmbedBuilder
        {
            Title = $"{creatorId}建立的預存字串共{presetTexts.Count}筆索引，指向{toTextCount}筆字串",
            Description = description.Length > 2048 ? description.Substring(0, 2040) + "..." : description
        };
        return ReplyToCommand(string.Empty, builder.Build());
    }
}