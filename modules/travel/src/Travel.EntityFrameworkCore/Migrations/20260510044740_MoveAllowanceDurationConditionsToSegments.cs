using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class MoveAllowanceDurationConditionsToSegments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppliesWhenTotalDaysFrom",
                table: "TrvAllowanceRuleSegments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AppliesWhenTotalDaysTo",
                table: "TrvAllowanceRuleSegments",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE segments
                SET
                    segments.AppliesWhenTotalDaysFrom = rules.AppliesWhenTotalDaysFrom,
                    segments.AppliesWhenTotalDaysTo = rules.AppliesWhenTotalDaysTo
                FROM TrvAllowanceRuleSegments segments
                INNER JOIN TrvAllowanceRules rules ON rules.Id = segments.AllowanceRuleId
                WHERE rules.AppliesWhenTotalDaysFrom IS NOT NULL
                   OR rules.AppliesWhenTotalDaysTo IS NOT NULL
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRuleSegments_AppliesWhenTotalDaysFrom",
                table: "TrvAllowanceRuleSegments",
                column: "AppliesWhenTotalDaysFrom");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRuleSegments_AppliesWhenTotalDaysTo",
                table: "TrvAllowanceRuleSegments",
                column: "AppliesWhenTotalDaysTo");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrvAllowanceRuleSegments_AppliesWhenTotalDaysFrom",
                table: "TrvAllowanceRuleSegments");

            migrationBuilder.DropIndex(
                name: "IX_TrvAllowanceRuleSegments_AppliesWhenTotalDaysTo",
                table: "TrvAllowanceRuleSegments");

            migrationBuilder.DropColumn(
                name: "AppliesWhenTotalDaysFrom",
                table: "TrvAllowanceRuleSegments");

            migrationBuilder.DropColumn(
                name: "AppliesWhenTotalDaysTo",
                table: "TrvAllowanceRuleSegments");

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
    }
}
