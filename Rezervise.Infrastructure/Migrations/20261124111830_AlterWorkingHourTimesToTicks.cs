using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rezervise.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlterWorkingHourTimesToTicks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "StartTime",
                table: "WorkingHours",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AlterColumn<long>(
                name: "EndTime",
                table: "WorkingHours",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AlterColumn<long>(
                name: "StartTime",
                table: "EmployeeWorkingHours",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AlterColumn<long>(
                name: "EndTime",
                table: "EmployeeWorkingHours",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AlterColumn<long>(
                name: "BreakStartTime",
                table: "EmployeeWorkingHours",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "time",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "BreakEndTime",
                table: "EmployeeWorkingHours",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "time",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeSpan>(
                name: "StartTime",
                table: "WorkingHours",
                type: "time",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "EndTime",
                table: "WorkingHours",
                type: "time",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "StartTime",
                table: "EmployeeWorkingHours",
                type: "time",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "EndTime",
                table: "EmployeeWorkingHours",
                type: "time",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "BreakStartTime",
                table: "EmployeeWorkingHours",
                type: "time",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "BreakEndTime",
                table: "EmployeeWorkingHours",
                type: "time",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
