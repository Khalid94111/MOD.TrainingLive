using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <summary>
    /// v4.7.0 — Phase 4A Patch 5 (Calculator Mode + Entity Rename + Funding Source Split).
    ///
    /// <para>
    /// Schema-only delta:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Renames <c>AppCasualCourseFinancials</c> → <c>AppCasualCourseFinancialItems</c>.</description></item>
    ///   <item><description>Renames FK column on <c>AppCasualCourseFinancialItemRanks</c>:
    ///     <c>CasualCourseFinancialId</c> → <c>CasualCourseFinancialItemId</c>.</description></item>
    ///   <item><description>Drops <c>AppCasualCourses.FundingSource</c> (Option II — destructive; existing values lost).</description></item>
    ///   <item><description>Adds <c>AppCasualCourses.FundingSourceName</c> (nvarchar(200), nullable),
    ///     <c>FundingSourceVoteCode</c> (nvarchar(100), nullable),
    ///     <c>CourseCost</c> (decimal(18,3), nullable).</description></item>
    ///   <item><description>Cleanup: deletes ALL existing <c>CasualCourseFinancialItemRanks</c> +
    ///     <c>CasualCourseFinancialItems</c> rows and clears <c>CasualCourses.EstimatedTotalCost</c>.
    ///     Patch 5 reassigns financial row creation to Staff (post-scenario-pick); rows
    ///     created during Patch 4's UTM-owns-financials experiment no longer match the
    ///     workflow, so we wipe them. The seeder rebuilds them on next startup.</description></item>
    /// </list>
    ///
    /// <para>
    /// Existing FK constraint and index names embedded in earlier migrations
    /// (e.g. <c>FK_AppCasualCourseFinancialItemRanks_AppCasualCourseFinancials_CasualCourseFinancialId</c>)
    /// remain in the DB after RenameTable + RenameColumn. They are functionally correct;
    /// rename them only if cosmetic alignment is later required.
    /// </para>
    /// </summary>
    public partial class v4_7_0_RenameAndExtendCasualCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Step 1 — wipe existing financial rows BEFORE the rename, so we don't
            //   leave orphans pointing at the old FK column name. ─────────────────
            migrationBuilder.Sql(@"
                DELETE FROM [AppCasualCourseFinancialItemRanks];
                DELETE FROM [AppCasualCourseFinancials];
                UPDATE [AppCasualCourses] SET [EstimatedTotalCost] = NULL;
            ");

            // ── Step 2 — rename the parent table. ───────────────────────────────
            migrationBuilder.RenameTable(
                name: "AppCasualCourseFinancials",
                newName: "AppCasualCourseFinancialItems");

            // ── Step 3 — rename the FK column on the rank child table. ──────────
            migrationBuilder.RenameColumn(
                name: "CasualCourseFinancialId",
                table: "AppCasualCourseFinancialItemRanks",
                newName: "CasualCourseFinancialItemId");

            // ── Step 4 — drop the old single-string FundingSource. ──────────────
            migrationBuilder.DropColumn(
                name: "FundingSource",
                table: "AppCasualCourses");

            // ── Step 5 — add the split funding-source columns + course cost. ────
            migrationBuilder.AddColumn<string>(
                name: "FundingSourceName",
                table: "AppCasualCourses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FundingSourceVoteCode",
                table: "AppCasualCourses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CourseCost",
                table: "AppCasualCourses",
                type: "decimal(18,3)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseCost",
                table: "AppCasualCourses");

            migrationBuilder.DropColumn(
                name: "FundingSourceVoteCode",
                table: "AppCasualCourses");

            migrationBuilder.DropColumn(
                name: "FundingSourceName",
                table: "AppCasualCourses");

            migrationBuilder.AddColumn<string>(
                name: "FundingSource",
                table: "AppCasualCourses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.RenameColumn(
                name: "CasualCourseFinancialItemId",
                table: "AppCasualCourseFinancialItemRanks",
                newName: "CasualCourseFinancialId");

            migrationBuilder.RenameTable(
                name: "AppCasualCourseFinancialItems",
                newName: "AppCasualCourseFinancials");
        }
    }
}
