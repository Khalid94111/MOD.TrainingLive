using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowanceRuleDurationConditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppliesWhenTotalDaysFrom",
                table: "TrvAllowanceRules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AppliesWhenTotalDaysTo",
                table: "TrvAllowanceRules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRules_AppliesWhenTotalDaysFrom",
                table: "TrvAllowanceRules",
                column: "AppliesWhenTotalDaysFrom");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRules_AppliesWhenTotalDaysTo",
                table: "TrvAllowanceRules",
                column: "AppliesWhenTotalDaysTo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrvAllowanceRules_AppliesWhenTotalDaysFrom",
                table: "TrvAllowanceRules");

            migrationBuilder.DropIndex(
                name: "IX_TrvAllowanceRules_AppliesWhenTotalDaysTo",
                table: "TrvAllowanceRules");

            migrationBuilder.DropColumn(
                name: "AppliesWhenTotalDaysFrom",
                table: "TrvAllowanceRules");

            migrationBuilder.DropColumn(
                name: "AppliesWhenTotalDaysTo",
                table: "TrvAllowanceRules");
        }
    }
}
