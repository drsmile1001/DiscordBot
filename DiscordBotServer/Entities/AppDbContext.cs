using Microsoft.EntityFrameworkCore;

namespace DiscordBotServer.Entities;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
        Database.Migrate();
    }

    public virtual DbSet<MessagePreset> MessagePreset { get; set; } = null!;

    public virtual DbSet<ReactsPreset> ReactsPreset { get; set; } = null!;

    public virtual DbSet<ShareLinkLog> ShareLinkLog { get; set; } = null!;
}
