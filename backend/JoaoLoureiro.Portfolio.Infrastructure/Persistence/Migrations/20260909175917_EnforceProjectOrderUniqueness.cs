using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoaoLoureiro.Portfolio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceProjectOrderUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Projects_SortOrder",
                table: "Projects");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_SortOrder",
                table: "Projects",
                column: "SortOrder",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Projects_SortOrder",
                table: "Projects");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_SortOrder",
                table: "Projects",
                column: "SortOrder");
        }
    }
}
