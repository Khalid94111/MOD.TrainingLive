using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeDailyAllowanceRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DailyAllowanceRate",
                table: "TrvTravelRequestEmployees",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeNumber",
                table: "TrvTravelRequestEmployees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequestEmployees_EmployeeNumber",
                table: "TrvTravelRequestEmployees",
                column: "EmployeeNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrvTravelRequestEmployees_EmployeeNumber",
                table: "TrvTravelRequestEmployees");

            migrationBuilder.DropColumn(
                name: "DailyAllowanceRate",
                table: "TrvTravelRequestEmployees");

            migrationBuilder.DropColumn(
                name: "EmployeeNumber",
                table: "TrvTravelRequestEmployees");
        }
    }
}
