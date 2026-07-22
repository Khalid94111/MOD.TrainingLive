using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowanceSnapshotDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AllowanceSnapshot_AccommodationMultiplier",
                table: "TrvTravelRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllowanceSnapshot_ClothingCalculationNote",
                table: "TrvTravelRequests",
                type: "nvarchar(max)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AllowanceSnapshot_DailyBaseRate",
                table: "TrvTravelRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllowanceSnapshot_EmployeeCalculationsJson",
                table: "TrvTravelRequests",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllowanceSnapshot_SegmentsJson",
                table: "TrvTravelRequests",
                type: "nvarchar(max)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AllowanceSnapshot_TotalDays",
                table: "TrvTravelRequests",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowanceSnapshot_AccommodationMultiplier",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "AllowanceSnapshot_ClothingCalculationNote",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "AllowanceSnapshot_DailyBaseRate",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "AllowanceSnapshot_EmployeeCalculationsJson",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "AllowanceSnapshot_SegmentsJson",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "AllowanceSnapshot_TotalDays",
                table: "TrvTravelRequests");
        }
    }
}
