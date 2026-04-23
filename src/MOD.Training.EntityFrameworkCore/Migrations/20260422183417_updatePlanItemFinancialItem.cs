using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class updatePlanItemFinancialItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppPlanItemFinancialItems_PlanItemId_FinancialItemId",
                table: "AppPlanItemFinancialItems");

            migrationBuilder.CreateIndex(
                name: "IX_AppPlanItemFinancialItems_PlanItemId_FinancialItemId",
                table: "AppPlanItemFinancialItems",
                columns: new[] { "PlanItemId", "FinancialItemId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppPlanItemFinancialItems_PlanItemId_FinancialItemId",
                table: "AppPlanItemFinancialItems");

            migrationBuilder.CreateIndex(
                name: "IX_AppPlanItemFinancialItems_PlanItemId_FinancialItemId",
                table: "AppPlanItemFinancialItems",
                columns: new[] { "PlanItemId", "FinancialItemId" },
                unique: true);
        }
    }
}
