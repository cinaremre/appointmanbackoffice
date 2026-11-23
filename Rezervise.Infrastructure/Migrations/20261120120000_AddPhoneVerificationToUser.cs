using System;
using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rezervise.Infrastructure.Migrations
{
    /// <summary>
    /// Telefon (OTP) doğrulama alanlarını Users tablosuna ekler.
    /// Ham kod saklanmaz; yalnızca SHA-256 hash'i tutulur.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261120120000_AddPhoneVerificationToUser")]
    public partial class AddPhoneVerificationToUser : Migration
    {
        /// <inheritdoç />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PhoneVerificationAttemptCount",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PhoneVerificationCodeExpiresAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneVerificationCodeHash",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PhoneVerificationRequestedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneVerificationCodeHash",
                table: "Users",
                column: "PhoneVerificationCodeHash",
                filter: "[PhoneVerificationCodeHash] IS NOT NULL");
        }

        /// <inheritdoç />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_PhoneVerificationCodeHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneVerificationAttemptCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneVerificationCodeExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneVerificationCodeHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneVerificationRequestedAt",
                table: "Users");
        }
    }
}
