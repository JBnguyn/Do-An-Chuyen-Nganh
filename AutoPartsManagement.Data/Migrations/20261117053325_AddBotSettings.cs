using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBotSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BotSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AiEnabled = table.Column<bool>(type: "bit", nullable: false),
                    BotName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BotPronoun = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CustomerPronoun = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Tone = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmojiLevel = table.Column<int>(type: "int", nullable: false),
                    WorkingHours = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Policies = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BotSettings");
        }
    }
}
