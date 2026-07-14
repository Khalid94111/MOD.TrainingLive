using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class FilterNominationUniqueIndexBySoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppNominations_PlanItemId_EmployeeId",
                table: "AppNominations");

            migrationBuilder.CreateIndex(
                name: "IX_AppNominations_PlanItemId_EmployeeId",
                table: "AppNominations",
                columns: new[] { "PlanItemId", "EmployeeId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppNominations_PlanItemId_EmployeeId",
                table: "AppNominations");

            migrationBuilder.CreateIndex(
                name: "IX_AppNominations_PlanItemId_EmployeeId",
                table: "AppNominations",
                columns: new[] { "PlanItemId", "EmployeeId" },
                unique: true);
        }
    }
}
