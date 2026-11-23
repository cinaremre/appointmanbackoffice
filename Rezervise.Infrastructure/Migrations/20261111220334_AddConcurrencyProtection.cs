using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rezervise.Infrastructure.Migrations
{
    /// <inheritdoç />
    public partial class AddConcurrencyProtection : Migration
    {
        /// <inheritdoç />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_AppointmentId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_BlacklistEntries_UserId_BusinessId",
                table: "BlacklistEntries");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_EmployeeId_AppointmentDate",
                table: "Appointments");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "WorkingHours",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Users",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Services",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ServiceCategoryTranslations",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ServiceCategories",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Reviews",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Notifications",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Favorites",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "EmployeeWorkingHours",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "EmployeeTimeOffs",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "EmployeeServices",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Employees",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "BusinessServiceCategories",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Businesses",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "BusinessCategories",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "BlacklistEntries",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Appointments",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AppointmentHistories",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Reviews_AppointmentId",
                table: "Reviews",
                column: "AppointmentId",
                unique: true,
                filter: "[AppointmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_BlacklistEntries_BusinessId_UserId",
                table: "BlacklistEntries",
                columns: new[] { "BusinessId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_EmployeeId_Date_End",
                table: "Appointments",
                columns: new[] { "EmployeeId", "AppointmentDate", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "UX_Appointments_EmployeeId_AppointmentDate_Active",
                table: "Appointments",
                columns: new[] { "EmployeeId", "AppointmentDate" },
                unique: true,
                filter: "[Status] <> 3 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_AppointmentId",
                table: "IdempotencyRecords",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "UX_IdempotencyRecords_Key",
                table: "IdempotencyRecords",
                column: "Key",
                unique: true);
        }

        /// <inheritdoç />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdempotencyRecords");

            migrationBuilder.DropIndex(
                name: "UX_Reviews_AppointmentId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "UX_BlacklistEntries_BusinessId_UserId",
                table: "BlacklistEntries");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_EmployeeId_Date_End",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "UX_Appointments_EmployeeId_AppointmentDate_Active",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "WorkingHours");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ServiceCategoryTranslations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ServiceCategories");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Favorites");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "EmployeeWorkingHours");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "EmployeeTimeOffs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "EmployeeServices");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "BusinessServiceCategories");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "BusinessCategories");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "BlacklistEntries");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AppointmentHistories");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_AppointmentId",
                table: "Reviews",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistEntries_UserId_BusinessId",
                table: "BlacklistEntries",
                columns: new[] { "UserId", "BusinessId" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_EmployeeId_AppointmentDate",
                table: "Appointments",
                columns: new[] { "EmployeeId", "AppointmentDate" });
        }
    }
}
