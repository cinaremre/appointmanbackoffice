using AppointMan.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointMan.Infrastructure.Migrations
{
    /// <summary>
    /// Favorites uzerindeki (UserId, BusinessId) unique index'ini filtreli hale getirir.
    /// Filtresiz index soft-delete edilmis kayitlarin anahtari isgal etmesine ve
    /// "favori cikarildi" sonrasi yeniden eklemenin 409 donmesine yol aciyordu.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261120130000_FixFavoriteUniqueIndex")]
    public partial class FixFavoriteUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Favorites_UserId_BusinessId",
                table: "Favorites");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId_BusinessId",
                table: "Favorites",
                columns: new[] { "UserId", "BusinessId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Favorites_UserId_BusinessId",
                table: "Favorites");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId_BusinessId",
                table: "Favorites",
                columns: new[] { "UserId", "BusinessId" },
                unique: true);
        }
    }
}
