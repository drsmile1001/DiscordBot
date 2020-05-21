using System;

namespace DiscordBotServer.Entities
{
    public class MessagePreset
    {
        /// <summary>
        /// ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 索引
        /// </summary>
        public string Index { get; set; }

        /// <summary>
        /// 重複索引的流水號
        /// </summary>
        public int SeriesNumber { get; set; }

        /// <summary>
        /// 儲存的字串
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 使用次數
        /// </summary>
        public int CalledCount { get; set; }

        /// <summary>
        /// 最後使用時間
        /// </summary>
        public DateTimeOffset LastCalledTime { get; set; }

        /// <summary>
        /// 最後的使用者
        /// </summary>
        public ulong LastCallerId { get; set; }

        /// <summary>
        /// 新增的使用者
        /// </summary>
        public ulong CreatorId { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTimeOffset CreatedTime { get; set; }
    }
}