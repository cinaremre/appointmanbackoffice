using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rezervise.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Businesses",
                type: "datetime2",
                nullable: true);

            // Mevcut satırlar (önceden çalışan işletmeler) onaylı sayılır:
            // defaultValue=true ile backfill yapılır. EF her eklemede kolonu
            // açıkça gönderdiği için DB default'u yalnızca bu backfill için kullanılır.
            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Businesses",
                type: "bit",
                nullable: false,
                defaultValue: true);

            // Onay zamanı da geriye dönük doldurulur; yeni kayıtlar Approve()
            // ile kendi zaman damgasını yazar.
            migrationBuilder.Sql(
                "UPDATE Businesses SET ApprovedAt = CreatedAt WHERE IsApproved = 1 AND ApprovedAt IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Businesses");
        }
    }
}
