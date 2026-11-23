using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rezervise.Infrastructure.Migrations
{
    /// <inheritdoç />
    public partial class AddEmailVerificationToUser : Migration
    {
        /// <inheritdoç />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationRequestedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationTokenExpiresAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationTokenHash",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailVerificationTokenHash",
                table: "Users",
                column: "EmailVerificationTokenHash",
                filter: "[EmailVerificationTokenHash] IS NOT NULL");

            // Geriye dönük uyumluluk: bu özellik devreye girmeden önce oluşturulmuş
            // tüm hesaplar doğrulanmış sayılır. Aksi halde mevcut kullanıcılar
            // (seed kullanıcıları dahil) giriş yapamaz hale gelir.
            migrationBuilder.Sql("UPDATE Users SET IsEmailVerified = 1 WHERE IsEmailVerified = 0;");
        }

        /// <inheritdoç />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_EmailVerificationTokenHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationRequestedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationTokenExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationTokenHash",
                table: "Users");
        }
    }
}
