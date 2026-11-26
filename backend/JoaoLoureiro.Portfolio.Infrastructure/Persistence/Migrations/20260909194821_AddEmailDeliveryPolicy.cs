using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoaoLoureiro.Portfolio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailDeliveryPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailDeliveryPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecipientAddress = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    SenderDisplayName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    SubjectPrefix = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Revision = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailDeliveryPolicies", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailDeliveryPolicies");
        }
    }
}
