using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBotServer.Migrations
{
    public partial class AddShareLinkLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShareLinkLog",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Link = table.Column<string>(type: "TEXT", nullable: false),
                    SenderId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    CreatedTimestamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareLinkLog", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShareLinkLog");
        }
    }
}
