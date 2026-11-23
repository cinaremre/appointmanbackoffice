using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rezervise.Infrastructure.Migrations
{
    /// <inheritdoç />
    public partial class AddUserLanguageCode : Migration
    {
        /// <inheritdoç />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LanguageCode",
                table: "Users",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "tr");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LanguageCode",
                table: "Users",
                column: "LanguageCode");
        }

        /// <inheritdoç />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_LanguageCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LanguageCode",
                table: "Users");
        }
    }
}
