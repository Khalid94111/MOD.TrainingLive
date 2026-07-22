using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowanceCategoryPerRank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "TrvTravelRequestEmployees",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "TrvAllowanceRules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "TrvAllowanceRates",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequestEmployees_Category",
                table: "TrvTravelRequestEmployees",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRules_Category",
                table: "TrvAllowanceRules",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRates_Category",
                table: "TrvAllowanceRates",
                column: "Category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrvTravelRequestEmployees_Category",
                table: "TrvTravelRequestEmployees");

            migrationBuilder.DropIndex(
                name: "IX_TrvAllowanceRules_Category",
                table: "TrvAllowanceRules");

            migrationBuilder.DropIndex(
                name: "IX_TrvAllowanceRates_Category",
                table: "TrvAllowanceRates");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "TrvTravelRequestEmployees");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "TrvAllowanceRules");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "TrvAllowanceRates");
        }
    }
}
