using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBotServer.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MessagePreset",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Index = table.Column<string>(type: "TEXT", nullable: false),
                    SeriesNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    CalledCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastCalledTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastCallerId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    CreatorId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    CreatedTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagePreset", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReactsPreset",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Reactions = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReactsPreset", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessagePreset");

            migrationBuilder.DropTable(
                name: "ReactsPreset");
        }
    }
}
