using System;
using LiteDB;

namespace DiscordBot.Model
{
    /// <summary>
    /// 預存Text
    /// </summary>
    public class PresetText
    {
        /// <summary>
        /// ID
        /// </summary>
        [BsonId]
        public Guid Id { get; set; }

        /// <summary>
        /// 查詢用的索引
        /// </summary>
        [BsonField]
        public string Index { get; set; }

        /// <summary>
        /// 多圖的流水號
        /// </summary>
        [BsonField]
        public int SubIndex { get; set; }

        /// <summary>
        /// 儲存的字串
        /// </summary>
        [BsonField]
        public string Text { get; set; }

        /// <summary>
        /// 使用次數
        /// </summary>
        [BsonField]
        public int UseCount { get; set; }

        /// <summary>
        /// 最後使用時間
        /// </summary>
        [BsonField]
        public DateTime LastUseTime { get; set; }

        /// <summary>
        /// 最後的使用者
        /// </summary>
        [BsonField]
        public string LastUser { get; set; }

        /// <summary>
        /// 新增的使用者
        /// </summary>
        [BsonField]
        public string CreateUser { get; set; }
    }
}
