using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBot.Model;
using F23.StringSimilarity;

namespace DiscordBot.Modules
{
    [Name("預存字串")]
    public class PresetTextModule : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        ///     內嵌內容一頁要顯示的筆數
        /// </summary>
        private const int TabLength = 10;

        public PresetTextModule(DiscordBotDb discordBotDb, Random random)
        {
            DiscordBotDb = discordBotDb;
            Random = random;
        }

        private DiscordBotDb DiscordBotDb { get; }

        private Random Random { get; }

        /// <summary>
        ///     相似度比對工具
        /// </summary>
        private JaroWinkler Jw { get; } = new JaroWinkler();

        /// <summary>
        ///     相似度門檻
        /// </summary>
        private double SimilarityThreshold { get; } = 0.6;

        /// <summary>
        ///     找到符合索引或接近索引的集合
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private (List<PresetText> founds, double? similarity) FoundPresetText(string index)
        {
            var founds = DiscordBotDb.Query<PresetText>()
                .Where(text => text.Index == index).ToList();
            if (founds.Count != 0)
                return (founds, null);

            var regularedIndex = Path.GetFileNameWithoutExtension(index);
            var extension = Path.GetExtension(index);
            var reverseIndex = new string(regularedIndex.Reverse().ToArray());

            var allPresetTextes = DiscordBotDb.Query<PresetText>().ToList();

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
                        Similarity = similarity * 0.5 + reverseSimilarity * 0.45 + extensionSimilarity * 0.05,
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
        private Task ReplyPresetText(PresetText found, double? foundSimilarity)
        {
            try
            {
                found.UseCount += 1;
                found.LastUseTime = DateTime.Now;
                found.LastUser = Context.User.Username;
                DiscordBotDb.Update(found);
            }
            catch (Exception e)
            {
                return ReplyAsync(e.Message);
            }

            return ReplyAsync(foundSimilarity != null
                ? $"{found.Index} ({foundSimilarity:P}) #{found.SubIndex} {found.Text}"
                : $"{found.Index} #{found.SubIndex} {found.Text}");
        }

        [Command("presetText")]
        [Alias("+")]
        [Summary("輸出儲存過的字串")]
        public Task PresetText([Summary("索引")] string index, [Summary("流水號")] [Optional] string subIndex)
        {
            var userName = Context.User.Username;
            var (founds, similarity) = FoundPresetText(index);
            if (founds == null)
                return ReplyAsync("沒有預存字串，加點水吧:sweat_drops:");
            if (similarity < SimilarityThreshold)
                return ReplyAsync("找不到接近的 怕:confounded:");

            PresetText found;

            //依據subindex再縮限
            if (subIndex != null)
            {
                if (!int.TryParse(subIndex, out var subIndexInt))
                    return ReplyAsync("流水號必須為整數");

                found = founds.FirstOrDefault(text => text.SubIndex == subIndexInt);
                if (found == null)
                    return ReplyAsync(similarity != null
                        ? $"找不到 {founds.First().Index} ({similarity:P}) #{subIndex}"
                        : $"找不到 {founds.First().Index} #{subIndex}");

                return ReplyPresetText(found, similarity);
            }

            //嘗試找到自己設定中最晚使用的
            found = founds.Where(text => text.CreateUser == userName)
                        .OrderByDescending(text => text.LastUseTime)
                        .FirstOrDefault()
                    //再嘗試找到所有人中最晚使用的
                    ?? founds.OrderByDescending(text => text.LastUseTime)
                        .First();
            return ReplyPresetText(found, similarity);
        }

        [Command("addPresetText")]
        [Alias("++")]
        [Summary("新增字串")]
        public Task AddPresetText([Summary("索引")] string index, [Summary("要儲存的字串")] [Remainder] string text)
        {
            var pool = DiscordBotDb.Query<PresetText>()
                .Where(presetText => presetText.Index == index)
                .ToList();
            var found = pool.Where(presetText => presetText.Text == text &&
                                                 presetText.CreateUser == Context.User.Username)
                .OrderByDescending(presetText => presetText.LastUseTime)
                .FirstOrDefault();

            if (found != null)
                return ReplyAsync($"已存在預存字串 {index} #{found.SubIndex}");

            var lastSubIndex = pool.Select(item => item.SubIndex).DefaultIfEmpty(-1).Max() + 1;
            try
            {
                DiscordBotDb.Insert(new PresetText
                {
                    Id = Guid.NewGuid(),
                    Index = index,
                    SubIndex = lastSubIndex,
                    Text = text,
                    UseCount = 0,
                    LastUseTime = DateTime.Now,
                    CreateUser = Context.User.Username,
                    LastUser = Context.User.Username
                });
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
            return $"顯示{(tabInt - 1) * TabLength + 1}-{(tabInt - 1) * TabLength + TabLength}";
        }

        [Command("detailPresetText")]
        [Alias("+?")]
        [Summary("查詢預存字串狀態，流水號可填寫\"_\"以查找字串池的其他分頁")]
        public Task DetailPresetText([Summary("索引")] string index, [Summary("流水號")] [Optional] string subIndex,
            [Summary("分頁")] [Optional] string tab)
        {
            if (subIndex == null || subIndex == "_")
            {
                var targets = DiscordBotDb.Query<PresetText>().Where(text => text.Index == index).ToList()
                    .OrderBy(item => item.SubIndex).ToList();
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

                foreach (var presetText in targets.Skip((tabInt - 1) * TabLength).Take(TabLength))
                    builder.AddField(x =>
                    {
                        x.Name = $"#{presetText.SubIndex}";
                        x.Value = $"字串:\"{presetText.Text}\"\n" +
                                  $"使用次數:{presetText.UseCount}\n" +
                                  $"最後使用時間:{presetText.LastUseTime}\n" +
                                  $"新增人:{presetText.CreateUser}";
                        x.IsInline = false;
                    });
                return ReplyAsync("", false, builder.Build());
            }
            if (!int.TryParse(subIndex, out var subIndexInt))
                return ReplyAsync("流水號應為整數");

            var found =
                DiscordBotDb.FirstOrDefault<PresetText>(
                    text => text.Index == index && text.SubIndex == subIndexInt);
            if (found == null)
                return ReplyAsync($"找不到 {index} #{subIndex}");
            var singleBuilder = new EmbedBuilder
            {
                Title = $"{index} #{subIndex}",
                Description = found.Text
            }.AddField(x =>
            {
                x.Name = found.Text;
                x.Value = $"使用次數:{found.UseCount}\n" +
                          $"最後使用時間:{found.LastUseTime}\n" +
                          $"新增人:{found.CreateUser}";
                x.IsInline = false;
            });
            return ReplyAsync("", false, singleBuilder.Build());
        }

        [Command("randomPresetText")]
        [Alias("+*")]
        [Summary("亂數輸出儲存過的字串")]
        public Task RandomPresetText([Summary("索引")] [Remainder] string index)
        {
            var (founds, similarity) = FoundPresetText(index);
            if (founds == null)
                return ReplyAsync("沒有預存字串，加點水吧:sweat_drops:");
            if (similarity < SimilarityThreshold)
                return ReplyAsync("找不到接近的 怕:confounded:");

            var pool = founds.AsParallel()
                .GroupBy(item => item.Text)
                .Select(grouping => grouping.OrderByDescending(item => item.LastUseTime).First())
                .ToList();

            var rdIndex = Random.Next(0, pool.Count);
            var found = pool[rdIndex];

            return ReplyPresetText(found, similarity);
        }

        [Command("listPresetText")]
        [Alias("+.")]
        [Summary("列出預存字串")]
        public Task ListPresetText([Summary("分頁")] [Optional] string tab)
        {
            if (!int.TryParse(tab, out var tabInt))
                tabInt = 1;

            var allPresetText = DiscordBotDb.Query<PresetText>().ToList();

            var source = allPresetText.AsParallel()
                .GroupBy(item => item.Text)
                .Select(
                    grouping =>
                    {
                        var title = grouping.OrderByDescending(item => item.UseCount).First();
                        var useCount = grouping.Sum(item => item.UseCount);
                        return new
                        {
                            Description = $"{title.Index} #{title.SubIndex}",
                            UseCount = useCount,
                            title.Text,
                            Grouping = grouping.Select(item => new
                            {
                                item.Index,
                                item.SubIndex
                            }).ToList()
                        };
                    }).OrderByDescending(item => item.UseCount).ToList();

            var builder = new EmbedBuilder
            {
                Description = $"可用的預存字串總數:{source.Count} {TabRangeDescription(tabInt)}"
            };

            foreach (var group in source.Skip((tabInt - 1) * TabLength).Take(TabLength))
                builder.AddField(x =>
                {
                    x.Name = group.Description;
                    x.Value = $"字串:\"{group.Text}\"\n" +
                              $"使用次數:{group.UseCount}\n"
                              + group.Grouping.Select(item => $"{item.Index} #{item.SubIndex}")
                                  .Aggregate((agg, next) => $"{agg}\n{next}");
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

            var found = DiscordBotDb.Query<PresetText>()
                .Where(item => item.Index == index && item.SubIndex == subIndexInt).FirstOrDefault();
            if (found == null)
                return ReplyAsync($"找不到 {index} #{subIndex}");
            if (found.CreateUser != Context.User.Username)
                return ReplyAsync($"必須由該字串的創建者{found.CreateUser}刪除");
            try
            {
                DiscordBotDb.Delete<PresetText>(found.Id);
                return ReplyAsync($"已刪除 {index} #{subIndex}");
            }
            catch (Exception e)
            {
                return ReplyAsync(e.Message);
            }
        }
    }
}