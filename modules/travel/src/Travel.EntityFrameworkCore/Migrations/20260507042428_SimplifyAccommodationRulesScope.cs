using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyAccommodationRulesScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrvAccommodationRules_Category",
                table: "TrvAccommodationRules");

            migrationBuilder.DropIndex(
                name: "IX_TrvAccommodationRules_TravelType",
                table: "TrvAccommodationRules");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "TrvAccommodationRules");

            migrationBuilder.DropColumn(
                name: "TravelType",
                table: "TrvAccommodationRules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "TrvAccommodationRules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TravelType",
                table: "TrvAccommodationRules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrvAccommodationRules_Category",
                table: "TrvAccommodationRules",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAccommodationRules_TravelType",
                table: "TrvAccommodationRules",
                column: "TravelType");
        }
    }
}
