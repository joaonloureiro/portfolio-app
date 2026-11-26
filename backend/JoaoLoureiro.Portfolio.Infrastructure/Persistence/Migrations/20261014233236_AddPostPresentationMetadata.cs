using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoaoLoureiro.Portfolio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPostPresentationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                table: "Posts",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TagsJson",
                table: "Posts",
                type: "TEXT",
                maxLength: 512,
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "CoverImageAlt",
                table: "PostLocalizedContent",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "TagsJson",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "CoverImageAlt",
                table: "PostLocalizedContent");
        }
    }
}
