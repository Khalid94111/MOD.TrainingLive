using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class AddAccommodationConditionToAllowanceRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AppliesWhenAccommodationIncluded",
                table: "TrvAllowanceRules",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRules_AppliesWhenAccommodationIncluded",
                table: "TrvAllowanceRules",
                column: "AppliesWhenAccommodationIncluded");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrvAllowanceRules_AppliesWhenAccommodationIncluded",
                table: "TrvAllowanceRules");

            migrationBuilder.DropColumn(
                name: "AppliesWhenAccommodationIncluded",
                table: "TrvAllowanceRules");
        }
    }
}
