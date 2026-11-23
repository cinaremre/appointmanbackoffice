using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rezervise.Infrastructure.Migrations
{
    /// <inheritdoç />
    public partial class AddAddressLocalityNeighborhood : Migration
    {
        /// <inheritdoç />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LocalityId",
                table: "Businesses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NeighborhoodId",
                table: "Businesses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_LocalityId",
                table: "Businesses",
                column: "LocalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_NeighborhoodId",
                table: "Businesses",
                column: "NeighborhoodId");

            migrationBuilder.AddForeignKey(
                name: "FK_Businesses_Localities_LocalityId",
                table: "Businesses",
                column: "LocalityId",
                principalTable: "Localities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Businesses_Neighborhoods_NeighborhoodId",
                table: "Businesses",
                column: "NeighborhoodId",
                principalTable: "Neighborhoods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoç />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Businesses_Localities_LocalityId",
                table: "Businesses");

            migrationBuilder.DropForeignKey(
                name: "FK_Businesses_Neighborhoods_NeighborhoodId",
                table: "Businesses");

            migrationBuilder.DropIndex(
                name: "IX_Businesses_LocalityId",
                table: "Businesses");

            migrationBuilder.DropIndex(
                name: "IX_Businesses_NeighborhoodId",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "LocalityId",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "NeighborhoodId",
                table: "Businesses");
        }
    }
}
