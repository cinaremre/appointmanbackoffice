using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rezervise.Infrastructure.Migrations
{
    /// <summary>
    /// Mesai kolonları time(7) → bigint (tick). SQL Server time ile bigint arasında
    /// ALTER COLUMN dönüşümü olmadığından (Error 206: operand type clash) kolonlar
    /// düşürülüp bigint olarak yeniden oluşturulur. Kolonlar migration sırasında
    /// henüz boştur (fresh DB); mevcut veritabanlarında mesai saatleri yeniden girilir.
    /// </summary>
    public partial class AlterWorkingHourTimesToTicks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            DropTimeAddLong(migrationBuilder, "WorkingHours", "StartTime", nullable: false);
            DropTimeAddLong(migrationBuilder, "WorkingHours", "EndTime", nullable: false);
            DropTimeAddLong(migrationBuilder, "EmployeeWorkingHours", "StartTime", nullable: false);
            DropTimeAddLong(migrationBuilder, "EmployeeWorkingHours", "EndTime", nullable: false);
            DropTimeAddLong(migrationBuilder, "EmployeeWorkingHours", "BreakStartTime", nullable: true);
            DropTimeAddLong(migrationBuilder, "EmployeeWorkingHours", "BreakEndTime", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropLongAddTime(migrationBuilder, "WorkingHours", "StartTime", nullable: false);
            DropLongAddTime(migrationBuilder, "WorkingHours", "EndTime", nullable: false);
            DropLongAddTime(migrationBuilder, "EmployeeWorkingHours", "StartTime", nullable: false);
            DropLongAddTime(migrationBuilder, "EmployeeWorkingHours", "EndTime", nullable: false);
            DropLongAddTime(migrationBuilder, "EmployeeWorkingHours", "BreakStartTime", nullable: true);
            DropLongAddTime(migrationBuilder, "EmployeeWorkingHours", "BreakEndTime", nullable: true);
        }

        private static void DropTimeAddLong(MigrationBuilder migrationBuilder, string table, string column, bool nullable)
        {
            migrationBuilder.DropColumn(name: column, table: table);
            migrationBuilder.AddColumn<long>(
                name: column,
                table: table,
                type: "bigint",
                nullable: nullable);
        }

        private static void DropLongAddTime(MigrationBuilder migrationBuilder, string table, string column, bool nullable)
        {
            migrationBuilder.DropColumn(name: column, table: table);
            migrationBuilder.AddColumn<TimeSpan>(
                name: column,
                table: table,
                type: "time",
                nullable: nullable);
        }
    }
}
