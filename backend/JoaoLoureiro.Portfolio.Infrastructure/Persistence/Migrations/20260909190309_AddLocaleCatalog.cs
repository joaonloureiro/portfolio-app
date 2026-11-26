using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoaoLoureiro.Portfolio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocaleCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocaleCatalogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Revision = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocaleCatalogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocaleCatalogEntries",
                columns: table => new
                {
                    CatalogId = table.Column<int>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                    Locale = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    ValueType = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    ScalarValue = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ListValueJson = table.Column<string>(type: "TEXT", maxLength: 8192, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocaleCatalogEntries", x => new { x.CatalogId, x.Key, x.Locale });
                    table.ForeignKey(
                        name: "FK_LocaleCatalogEntries_LocaleCatalogs_CatalogId",
                        column: x => x.CatalogId,
                        principalTable: "LocaleCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocaleCatalogEntries");

            migrationBuilder.DropTable(
                name: "LocaleCatalogs");
        }
    }
}
