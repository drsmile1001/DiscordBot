using System;
using System.IO;
using LiteDB;

namespace DiscordBot.Model
{
    public class DiscordBotDb : LiteRepository
    {
        public DiscordBotDb() : base(new ConnectionString
        {
            Filename = Path.Combine(AppContext.BaseDirectory, "discordBotDb.db"),
            LimitSize = 1024 * 1024 * 100
        })
        {
        }
    }
}