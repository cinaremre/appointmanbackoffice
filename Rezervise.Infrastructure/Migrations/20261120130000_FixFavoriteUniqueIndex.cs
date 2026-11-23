using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rezervise.Infrastructure.Migrations
{
    /// <summary>
    /// Favorites üzerindeki (UserId, BusinessId) unique index'ini filtreli hale getirir.
    /// Filtresiz index soft-delete edilmiş kayıtların anahtarı işgal etmesine ve
    /// "favori çıkarıldı" sonrası yeniden eklemenin 409 dönmesine yol açıyordu.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261120130000_FixFavoriteUniqueIndex")]
    public partial class FixFavoriteUniqueIndex : Migration
    {
        /// <inheritdoç />
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

        /// <inheritdoç />
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
