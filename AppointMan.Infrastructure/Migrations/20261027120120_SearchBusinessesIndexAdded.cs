using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointMan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SearchBusinessesIndexAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Businesses_CategoryId",
                table: "Businesses");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_CategoryId_IsActive_IsVerified",
                table: "Businesses",
                columns: new[] { "CategoryId", "IsActive", "IsVerified" });

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_City",
                table: "Businesses",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_City_District",
                table: "Businesses",
                columns: new[] { "City", "District" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Businesses_CategoryId_IsActive_IsVerified",
                table: "Businesses");

            migrationBuilder.DropIndex(
                name: "IX_Businesses_City",
                table: "Businesses");

            migrationBuilder.DropIndex(
                name: "IX_Businesses_City_District",
                table: "Businesses");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_CategoryId",
                table: "Businesses",
                column: "CategoryId");
        }
    }
}
