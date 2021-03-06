﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBotServer.Entities;
using F23.StringSimilarity;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBotServer.Modules
{
    [Name("預設訊息")]
    public class MessagePresetModule : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        ///     內嵌內容一頁要顯示的筆數
        /// </summary>
        private const int _tabLength = 10;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Random _random;

        /// <summary>
        ///     相似度門檻
        /// </summary>
        private const double _similarityThreshold = 0.6;

        public MessagePresetModule(IServiceScopeFactory scopeFactory, Random random)
        {
            _scopeFactory = scopeFactory;
            _random = random;
        }

        /// <summary>
        ///     相似度比對工具
        /// </summary>
        private JaroWinkler Jw { get; } = new JaroWinkler();

        /// <summary>
        ///     找到符合索引或接近索引的集合
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private (List<MessagePreset> founds, double? similarity) FoundPresetText(string index)
        {
            using var scope = _scopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var founds = db.MessagePreset
                .AsQueryable()
                .Where(text => text.Index == index)
                .ToList();
            if (founds.Count != 0)
                return (founds, null);

            var regularedIndex = Path.GetFileNameWithoutExtension(index);
            var extension = Path.GetExtension(index);
            var reverseIndex = new string(regularedIndex.Reverse().ToArray());

            var allPresetTextes = db.MessagePreset.ToList();

            var foundsBySmilar = allPresetTextes
                .GroupBy(item => item.Index)
                .AsParallel()
                .Select(indexGrouping =>
                {
                    var sampleIndex = indexGrouping.Key;

                    var regularedSampleIndex = Path.GetFileNameWithoutExtension(sampleIndex);
                    var sampleExtension = Path.GetExtension(sampleIndex);
                    var reverseSampleIndex = new string(regularedSampleIndex.Reverse().ToArray());

                    var similarity = Jw.Similarity(regularedIndex, regularedSampleIndex);
                    var extensionSimilarity = Jw.Similarity(extension, sampleExtension);
                    var reverseSimilarity = Jw.Similarity(reverseIndex, reverseSampleIndex);

                    return new
                    {
                        Similarity = similarity * 0.49 + reverseSimilarity * 0.49 + extensionSimilarity * 0.02,
                        IndexGrouping = indexGrouping
                    };
                })
                .OrderByDescending(item => item.Similarity)
                .FirstOrDefault();

            return (foundsBySmilar?.IndexGrouping.ToList(), foundsBySmilar?.Similarity);
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
                return ReplyAsync(e.Message);
            }

            return ReplyAsync(foundSimilarity != null
                ? $"{found.Index} ({foundSimilarity:P}) #{found.SeriesNumber} {found.Text}"
                : $"{found.Index} #{found.SeriesNumber} {found.Text}");
        }

        [Command("presetText")]
        [Alias("+")]
        [Summary("輸出儲存過的字串")]
        public Task PresetText([Summary("索引")] string index, [Summary("流水號")] [Optional] string subIndex)
        {
            var userId = Context.User.Id;
            var (founds, similarity) = FoundPresetText(index);
            if (founds == null)
                return ReplyAsync("沒有預存字串，加點水吧:sweat_drops:");
            if (similarity < _similarityThreshold)
                return ReplyAsync("找不到接近的 怕:confounded:");

            MessagePreset found;

            //依據subindex再縮限
            if (subIndex != null)
            {
                if (!int.TryParse(subIndex, out var subIndexInt))
                    return ReplyAsync("流水號必須為整數");

                found = founds.FirstOrDefault(text => text.SeriesNumber == subIndexInt);
                if (found == null)
                    return ReplyAsync(similarity != null
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
        public Task AddPresetText([Summary("索引")] string index, [Summary("要儲存的字串")] [Remainder] string text)
        {
            using var scope = _scopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pool = db.MessagePreset
                .AsQueryable()
                .Where(presetText => presetText.Index == index)
                .ToList();
            var found = pool.Where(presetText => presetText.Text == text &&
                                                 presetText.CreatorId == Context.User.Id)
                .OrderByDescending(presetText => presetText.LastCalledTime)
                .FirstOrDefault();

            if (found != null)
                return ReplyAsync($"已存在預存字串 {index} #{found.SeriesNumber}");

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
                    LastCalledTime = DateTimeOffset.UtcNow,
                    CreatorId = Context.User.Id,
                    LastCallerId = Context.User.Id,
                    CreatedTime = DateTimeOffset.UtcNow
                });
                db.SaveChanges();
                var builder = new EmbedBuilder
                {
                    Title = $"已新增預存字串 {index} #{lastSubIndex}",
                    Description = text
                };
                return ReplyAsync("", false, builder.Build());
            }
            catch (Exception ex)
            {
                return ReplyAsync($"新增時發生錯誤：{ex.Message}");
            }
        }

        /// <summary>
        ///     分頁範圍字串
        /// </summary>
        /// <param name="tabInt"></param>
        /// <returns></returns>
        private string TabRangeDescription(int tabInt)
        {
            return $"顯示{(tabInt - 1) * _tabLength + 1}-{(tabInt - 1) * _tabLength + _tabLength}";
        }

        [Command("detailPresetText")]
        [Alias("+?")]
        [Summary("查詢預存字串狀態，流水號可填寫\"_\"以查找字串池的其他分頁")]
        public Task DetailPresetText([Summary("索引")] string index, [Summary("流水號")] [Optional] string subIndex,
            [Summary("分頁")] [Optional] string tab)
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
                    return ReplyAsync($"沒有預存字串的索引為{index}");
                if (!int.TryParse(tab, out var tabInt))
                    tabInt = 1;

                var builder = new EmbedBuilder
                {
                    Title = index,
                    Description = $"共{targets.Count}筆",
                    Footer = new EmbedFooterBuilder().WithText(TabRangeDescription(tabInt))
                };

                foreach (var presetText in targets.Skip((tabInt - 1) * _tabLength).Take(_tabLength))
                    builder.AddField(x =>
                    {
                        x.Name = $"#{presetText.SeriesNumber}";
                        x.Value = $"字串:\"{presetText.Text}\"\n" +
                                  $"使用次數:{presetText.CalledCount}\n" +
                                  $"最後使用時間:{presetText.LastCalledTime.LocalDateTime}\n" +
                                  $"新增人:<@{presetText.CreatorId}>\n" +
                                  $"新增時間:{presetText.CreatedTime.LocalDateTime}";
                        x.IsInline = false;
                    });
                return ReplyAsync("", false, builder.Build());
            }
            if (!int.TryParse(subIndex, out var subIndexInt))
                return ReplyAsync("流水號應為整數");

            var found = db.MessagePreset.FirstOrDefault(text => text.Index == index && text.SeriesNumber == subIndexInt);
            if (found == null)
                return ReplyAsync($"找不到 {index} #{subIndex}");
            var singleBuilder = new EmbedBuilder
            {
                Title = $"{index} #{subIndex}",
                Description = found.Text
            }.AddField(x =>
            {
                x.Name = found.Text;
                x.Value = $"使用次數:{found.CalledCount}\n" +
                          $"最後使用時間:{found.LastCalledTime.LocalDateTime}\n" +
                          $"新增人:<@{found.CreatorId}>\n" +
                          $"新增時間:{found.CreatedTime.LocalDateTime}";
                x.IsInline = false;
            });
            return ReplyAsync("", false, singleBuilder.Build());
        }

        [Command("editPresetText")]
        [Alias("+=")]
        [Summary("編輯預存字串索引")]
        public Task EditPresetText([Summary("索引")] string index, [Summary("流水號")] string subIndex,
            [Summary("新索引")] string newIndex)
        {
            if (!int.TryParse(subIndex, out var subIndexInt))
                return ReplyAsync("流水號應為整數");
            using var scope = _scopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var found = db.MessagePreset.FirstOrDefault(text => text.Index == index && text.SeriesNumber == subIndexInt);
            if (found == null)
                return ReplyAsync($"找不到 {index} #{subIndex}");

            var newSeriesNumber = db.MessagePreset.AsQueryable()
                .Where(presetText => presetText.Index == newIndex)
                .Select(preset=>preset.SeriesNumber)
                .DefaultIfEmpty(-1)
                .Max() + 1;
            found.Index = newIndex;
            found.SeriesNumber = newSeriesNumber;
            db.Update(found);
            db.SaveChanges();
            return ReplyAsync($"已更新[{index} #{subIndex}]為[{newIndex} #{newSeriesNumber}]");
        }

        [Command("randomPresetText")]
        [Alias("+*")]
        [Summary("亂數輸出儲存過的字串")]
        public Task RandomPresetText([Summary("索引")] [Remainder] string index)
        {
            var (founds, similarity) = FoundPresetText(index);
            if (founds == null)
                return ReplyAsync("沒有預存字串，加點水吧:sweat_drops:");
            if (similarity < _similarityThreshold)
                return ReplyAsync("找不到接近的 怕:confounded:");

            var found = founds.AsParallel()
                .GroupBy(item => item.Text)
                .Select(grouping => grouping.OrderByDescending(item => item.LastCalledTime).First())
                .OrderByDescending(item => item.LastCalledTime)
                .Select((item, i) => new
                {
                    Weights = (i * 0.9 / founds.Count + 0.1) * _random.NextDouble(),
                    PresetText = item
                })
                .OrderByDescending(item => item.Weights)
                .FirstOrDefault().PresetText;
            return ReplyPresetText(found, similarity);
        }

        [Command("listPresetText")]
        [Alias("+.")]
        [Summary("列出預存字串")]
        public Task ListPresetText([Summary("分頁")] [Optional] string tab, [Optional] string index)
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
                        ? $"{group.Text.Substring(0, allowLength)}..."
                        : group.Text;
                    x.Value = $"字串:{showText}\n" + meta;
                    x.IsInline = false;
                });
            return ReplyAsync("", false, builder.Build());
        }

        [Command("deletePresetText")]
        [Alias("+-")]
        [Summary("刪除預存字串")]
        public Task DeletePresetText([Summary("索引")] string index, [Summary("流水號")] string subIndex)
        {
            if (!int.TryParse(subIndex, out var subIndexInt))
                return ReplyAsync("必須輸入整數流水號");
            using var scope = _scopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var found = db.MessagePreset.AsQueryable()
                .Where(item => item.Index == index && item.SeriesNumber == subIndexInt)
                .FirstOrDefault();
            if (found == null)
                return ReplyAsync($"找不到 {index} #{subIndex}");
            if (found.CreatorId != Context.User.Id)
                return ReplyAsync($"必須由該字串的創建者<@{found.CreatorId}>刪除");
            try
            {
                db.MessagePreset.Remove(found);
                db.SaveChanges();
                return ReplyAsync($"已刪除 {index} #{subIndex}");
            }
            catch (Exception e)
            {
                return ReplyAsync(e.Message);
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
            var creatorGroupingBy = allPresetText.GroupBy(text => text.CreatorId).Select(grouping => new
                {
                    Creator = $"<@{grouping.Key}>",
                    Count = grouping.Count()
                })
                .OrderByDescending(item => item.Count)
                .ToList();
            foreach (var item in creatorGroupingBy)
                builder.AddField(item.Creator, $"{item.Count}筆", true);
            return ReplyAsync("", false, builder.Build());
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
                return ReplyAsync("查無此使用者或此使用者未建立任何預存字串");
            var toTextCount = presetTexts.Select(text => text.Text).Distinct().Count();
            var description = string.Join(',', presetTexts.OrderByDescending(text=>text.CalledCount)
                .Select(text => $"{text.Index}#{text.SeriesNumber}"));
            var builder = new EmbedBuilder
            {
                Title = $"{creatorId}建立的預存字串共{presetTexts.Count}筆索引，指向{toTextCount}筆字串",
                Description = description.Length > 2048? description.Substring(0,2040) + "..." : description
            };
            return ReplyAsync("", false, builder.Build());
        }
    }
}