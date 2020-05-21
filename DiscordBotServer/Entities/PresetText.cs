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
        /// 查詢用的索引
        /// </summary>
        public string Index { get; set; }

        /// <summary>
        /// 重複索引的流水號
        /// </summary>
        public int SubIndex { get; set; }

        /// <summary>
        /// 儲存的字串
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 使用次數
        /// </summary>
        public int UseCount { get; set; }

        /// <summary>
        /// 最後使用時間
        /// </summary>
        public DateTime LastUseTime { get; set; }

        /// <summary>
        /// 最後的使用者
        /// </summary>
        public string LastUser { get; set; }

        /// <summary>
        /// 新增的使用者
        /// </summary>
        public string CreateUser { get; set; }
    }
}