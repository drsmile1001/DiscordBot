using System.IO;
using LiteDB;

namespace DiscordBot.Model
{
    public class DiscordBotDb : LiteRepository
    {
        public DiscordBotDb(Stream stream, BsonMapper mapper = null, string password = null) : base(stream, mapper, password)
        {
        }
    }
}