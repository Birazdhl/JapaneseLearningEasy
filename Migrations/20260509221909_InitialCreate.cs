using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JapaneseLearningApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LastDatabaseImportUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppMetadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JapaneseTable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    English = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Romaji = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Japanese = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JapaneseTable", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AppMetadata",
                columns: new[] { "Id", "LastDatabaseImportUtc" },
                values: new object[] { 1, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppMetadata");

            migrationBuilder.DropTable(
                name: "JapaneseTable");
        }
    }
}
