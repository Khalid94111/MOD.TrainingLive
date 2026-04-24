using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class addFilterInCasualCourseFinancial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppCasualCourseFinancials_CasualCourseId_FinancialItemId",
                table: "AppCasualCourseFinancials");

            migrationBuilder.CreateIndex(
                name: "IX_AppCasualCourseFinancials_CasualCourseId_FinancialItemId",
                table: "AppCasualCourseFinancials",
                columns: new[] { "CasualCourseId", "FinancialItemId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppCasualCourseFinancials_CasualCourseId_FinancialItemId",
                table: "AppCasualCourseFinancials");

            migrationBuilder.CreateIndex(
                name: "IX_AppCasualCourseFinancials_CasualCourseId_FinancialItemId",
                table: "AppCasualCourseFinancials",
                columns: new[] { "CasualCourseId", "FinancialItemId" },
                unique: true);
        }
    }
}
