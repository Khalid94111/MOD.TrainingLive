using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class v4_5_3_FundingScenarioFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Step 1 — add the classification column ────────────────────────────
            migrationBuilder.AddColumn<int>(
                name: "ItemType",
                table: "TrnFinancialItems",
                type: "int",
                nullable: true,
                defaultValue: null);

            // ── Step 2 — fuzzy-match backfill by Arabic leaf name ─────────────────
            // Covers the names seeded by TenantDataSeeder as well as the prompt's
            // reference names (e.g. 'علاوة سفر' vs 'بدل السفر'). Parent rows matched
            // here are reset to NULL in step 3.
//            migrationBuilder.Sql(@"
//UPDATE [dbo].[TrnFinancialItems] SET ItemType = 1
//    WHERE (NameAr LIKE N'%تكلفة الدورة%' OR NameAr LIKE N'%تكلفة الدورات%');

//UPDATE [dbo].[TrnFinancialItems] SET ItemType = 2
//    WHERE (NameAr LIKE N'%تذكرة%' OR NameAr LIKE N'%تذاكر%' OR NameAr LIKE N'%التذاكر%');

//UPDATE [dbo].[TrnFinancialItems] SET ItemType = 3
//    WHERE NameAr LIKE N'%تأمين%';

//UPDATE [dbo].[TrnFinancialItems] SET ItemType = 4
//    WHERE NameAr LIKE N'%تأشيرة%';

//UPDATE [dbo].[TrnFinancialItems] SET ItemType = 5
//    WHERE (NameAr LIKE N'%علاوة سفر%' OR NameAr LIKE N'%بدل سفر%' OR NameAr LIKE N'%بدل السفر%');

//UPDATE [dbo].[TrnFinancialItems] SET ItemType = 6
//    WHERE NameAr LIKE N'%ملابس%';
//");

//            // ── Step 3 — defensive: parent rows must remain NULL ──────────────────
//            migrationBuilder.Sql(@"
//UPDATE [dbo].[TrnFinancialItems]
//SET    ItemType = NULL
//WHERE  Id IN (SELECT DISTINCT ParentId FROM [dbo].[TrnFinancialItems] WHERE ParentId IS NOT NULL);
//");

            // ── Step 4 — recompute Source on existing CasualCourseFinancial rows ──
            // Before this patch every row carried Source = 0 regardless of scenario.
            // Re-derive from (CasualCourse.FundingScenario × FinancialItem.ItemType):
            //
            //   Scenario 1 (FundingSourceCoversAll = 1) → FundingSource (0)   for all
            //   Scenario 3 (FinancialItemsCoverAll  = 3) → FinancialItem (1)  for all
            //   Scenario 2 (FundingSourceCoversCourse = 2)
            //               → ItemType = CourseCost (1) → FundingSource (0)
            //               → otherwise                  → FinancialItem (1)
            //
            // Rows whose course has no scenario yet are left untouched. NULL ItemType
            // under scenario 2 falls to FinancialItem — the safe default.
//            migrationBuilder.Sql(@"
//UPDATE ccf
//SET    Source = CASE
//                    WHEN cc.FundingScenario = 1 THEN 0
//                    WHEN cc.FundingScenario = 3 THEN 1
//                    WHEN cc.FundingScenario = 2 THEN
//                        CASE WHEN fi.ItemType = 1 THEN 0 ELSE 1 END
//                    ELSE ccf.Source
//                END
//FROM   [dbo].[AppCasualCourseFinancials] ccf
//INNER JOIN [dbo].[AppCasualCourses]      cc ON cc.Id = ccf.CasualCourseId
//INNER JOIN [dbo].[TrnFinancialItems]     fi ON fi.Id = ccf.FinancialItemId
//WHERE  cc.FundingScenario IS NOT NULL
//  AND  ccf.IsDeleted = 0
//  AND  cc.IsDeleted  = 0;
//");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Source values touched by the backfill are NOT reverted on Down — there's
            // no record of their pre-patch values. Drop the column only.
            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "TrnFinancialItems");
        }
    }
}
