using System.IO;
using LiteDB;

namespace DiscordBot.Model
{
    public class DiscordBotDb : LiteRepository
    {
        public DiscordBotDb(Stream stream, BsonMapper mapper = null, string password = null) : base(stream, mapper, password)
        {
            var PresetText = Database.GetCollection<PresetText>();
            PresetText.EnsureIndex(item => item.Index);
            PresetText.EnsureIndex(item => item.SubIndex);
            PresetText.EnsureIndex(item => item.LastUseTime);
            PresetText.EnsureIndex(item => item.CreateUser);
        }
    }
}