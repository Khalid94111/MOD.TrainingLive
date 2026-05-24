using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class v4_10_0_AnnualPlanSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ──────────────────────────────────────────────────────────────────
            // Phase 4C-α (v4.10.0) — Annual Plan Sessions Execution layer.
            //
            // CourseSession is reshaped from the Phase-3 "self-contained scheduling"
            // shape (CourseId+SessionCode+Location+MaxSeats+Cost+CompletionStatus)
            // into the Phase 4C-α "polymorphic-source execution" shape (TrainingPlanItemId
            // XOR TrainingCenterPlanItemId + TenantCourseId + nullable ActualStart/End +
            // PreferredQuarter + PlanYear + cancellation triplet + SelectedPriceQuoteId).
            //
            // The legacy columns (SessionCode/MaxSeats/AvailableSeats/Cost/Location/
            // Country/StartDate/EndDate/CompletionStatus/CompletedAt/CourseId) are
            // semantically incompatible with the new shape — EF's auto-scaffolder
            // suggested renames (MaxSeats→PreferredQuarter, CompletedAt→CancelledAt,
            // AvailableSeats→CourseType, CompletionStatus→PlanYear) that would carry
            // garbage data, so this migration drops them outright and adds the new
            // columns fresh.
            //
            // Defensive drain: per checkpoint v4.9.2 no real session data exists, but
            // dev-seed fixtures may. Clear all session-bound dependents and the
            // sessions themselves before touching the schema so the new polymorphic
            // CHECK constraint can be applied to an empty table without violation.
            // ──────────────────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                DELETE FROM [AppTravelAllowancePayments] WHERE [SessionId] IS NOT NULL;
                DELETE FROM [AppCoursePayments]          WHERE [SessionId] IS NOT NULL;
                DELETE FROM [AppTravelInstructions]      WHERE [SessionId] IS NOT NULL;
                DELETE FROM [AppPriceQuotes]             WHERE [SessionId] IS NOT NULL;
                UPDATE      [AppNominations]      SET [SessionId] = NULL WHERE [SessionId] IS NOT NULL;
                DELETE FROM [AppSessionConditions];
                DELETE FROM [AppCourseSessions];
            ");

            // ── CourseSession table reshape ────────────────────────────────
            migrationBuilder.DropForeignKey(
                name: "FK_AppCourseSessions_AppCourses_CourseId",
                table: "AppCourseSessions");

            migrationBuilder.DropIndex(
                name: "IX_AppCourseSessions_CourseId",
                table: "AppCourseSessions");

            migrationBuilder.DropIndex(
                name: "IX_AppCourseSessions_SessionCode",
                table: "AppCourseSessions");

            // Drop the Phase-3-only columns. Data is already cleared above.
            migrationBuilder.DropColumn(name: "AvailableSeats",    table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "CompletedAt",       table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "CompletionStatus",  table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "Cost",              table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "Country",           table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "CourseId",          table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "EndDate",           table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "Location",          table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "MaxSeats",          table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "SessionCode",       table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "StartDate",         table: "AppCourseSessions");

            // SessionStatus enum renumbers from {Scheduled=0..Cancelled=3} to
            // {Planned=0..FinanciallyClosed=5}. Default becomes 0 (Planned). The drain
            // above ensures no stale Status values survive.
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "AppCourseSessions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            // FullAuditedAggregateRoot adds ExtraProperties + ConcurrencyStamp.
            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "AppCourseSessions",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExtraProperties",
                table: "AppCourseSessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            // New shape: polymorphic source, snapshotted TenantCourse+CourseType, optional
            // ActualStart/End, preferred-quarter triplet, winning-quote pointer, cancellation triplet.
            migrationBuilder.AddColumn<Guid>(
                name: "TrainingPlanItemId",
                table: "AppCourseSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TrainingCenterPlanItemId",
                table: "AppCourseSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantCourseId",
                table: "AppCourseSessions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<int>(
                name: "CourseType",
                table: "AppCourseSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PreferredQuarter",
                table: "AppCourseSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlanYear",
                table: "AppCourseSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualStartDate",
                table: "AppCourseSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualEndDate",
                table: "AppCourseSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SelectedPriceQuoteId",
                table: "AppCourseSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "AppCourseSessions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "AppCourseSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CancelledById",
                table: "AppCourseSessions",
                type: "uniqueidentifier",
                nullable: true);

            // ── Indexes ────────────────────────────────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_AppCourseSessions_TenantId_TrainingPlanItemId",
                table: "AppCourseSessions",
                columns: new[] { "TenantId", "TrainingPlanItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppCourseSessions_TenantId_TrainingCenterPlanItemId",
                table: "AppCourseSessions",
                columns: new[] { "TenantId", "TrainingCenterPlanItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppCourseSessions_TenantId_TenantCourseId",
                table: "AppCourseSessions",
                columns: new[] { "TenantId", "TenantCourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppCourseSessions_TenantId_Status",
                table: "AppCourseSessions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppCourseSessions_TenantId_PlanYear_PreferredQuarter",
                table: "AppCourseSessions",
                columns: new[] { "TenantId", "PlanYear", "PreferredQuarter" });

            // ── Polymorphic XOR check constraint ───────────────────────────
            migrationBuilder.AddCheckConstraint(
                name: "CK_CourseSession_PolymorphicSource",
                table: "AppCourseSessions",
                sql: "([TrainingPlanItemId] IS NOT NULL AND [TrainingCenterPlanItemId] IS NULL) OR ([TrainingPlanItemId] IS NULL AND [TrainingCenterPlanItemId] IS NOT NULL)");

            // ── SessionNomination — new table for Phase 4C-α nominee snapshots ──
            migrationBuilder.CreateTable(
                name: "AppSessionNominations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubstitutionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSessionNominations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSessionNominations_AppCourseSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AppCourseSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppSessionNominations_SessionId",
                table: "AppSessionNominations",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSessionNominations_TenantId_SessionId",
                table: "AppSessionNominations",
                columns: new[] { "TenantId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppSessionNominations_TenantId_SessionId_EmployeeId",
                table: "AppSessionNominations",
                columns: new[] { "TenantId", "SessionId", "EmployeeId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ── Roll back SessionNomination ────────────────────────────────
            migrationBuilder.DropTable(name: "AppSessionNominations");

            // ── Roll back CourseSession reshape ────────────────────────────
            migrationBuilder.DropCheckConstraint(
                name: "CK_CourseSession_PolymorphicSource",
                table: "AppCourseSessions");

            migrationBuilder.DropIndex(name: "IX_AppCourseSessions_TenantId_PlanYear_PreferredQuarter", table: "AppCourseSessions");
            migrationBuilder.DropIndex(name: "IX_AppCourseSessions_TenantId_Status",                    table: "AppCourseSessions");
            migrationBuilder.DropIndex(name: "IX_AppCourseSessions_TenantId_TenantCourseId",            table: "AppCourseSessions");
            migrationBuilder.DropIndex(name: "IX_AppCourseSessions_TenantId_TrainingCenterPlanItemId",  table: "AppCourseSessions");
            migrationBuilder.DropIndex(name: "IX_AppCourseSessions_TenantId_TrainingPlanItemId",        table: "AppCourseSessions");

            migrationBuilder.DropColumn(name: "ActualEndDate",             table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "ActualStartDate",           table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "CancellationReason",        table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "CancelledAt",               table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "CancelledById",             table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "ConcurrencyStamp",          table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "CourseType",                table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "ExtraProperties",           table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "PlanYear",                  table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "PreferredQuarter",          table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "SelectedPriceQuoteId",      table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "TenantCourseId",            table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "TrainingCenterPlanItemId",  table: "AppCourseSessions");
            migrationBuilder.DropColumn(name: "TrainingPlanItemId",        table: "AppCourseSessions");

            // Status renumber back: default returns to 0 (Scheduled in the old enum).
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "AppCourseSessions",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            // Re-add the Phase-3 columns with their previous types.
            migrationBuilder.AddColumn<int>(name: "AvailableSeats", table: "AppCourseSessions",
                type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<DateTime>(name: "CompletedAt", table: "AppCourseSessions",
                type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<int>(name: "CompletionStatus", table: "AppCourseSessions",
                type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<decimal>(name: "Cost", table: "AppCourseSessions",
                type: "decimal(18,3)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Country", table: "AppCourseSessions",
                type: "nvarchar(256)", maxLength: 256, nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "CourseId", table: "AppCourseSessions",
                type: "uniqueidentifier", nullable: false, defaultValue: Guid.Empty);
            migrationBuilder.AddColumn<DateTime>(name: "EndDate", table: "AppCourseSessions",
                type: "datetime2", nullable: false, defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
            migrationBuilder.AddColumn<string>(name: "Location", table: "AppCourseSessions",
                type: "nvarchar(256)", maxLength: 256, nullable: true);
            migrationBuilder.AddColumn<int>(name: "MaxSeats", table: "AppCourseSessions",
                type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<string>(name: "SessionCode", table: "AppCourseSessions",
                type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<DateTime>(name: "StartDate", table: "AppCourseSessions",
                type: "datetime2", nullable: false, defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_AppCourseSessions_CourseId",
                table: "AppCourseSessions",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCourseSessions_SessionCode",
                table: "AppCourseSessions",
                column: "SessionCode");

            migrationBuilder.AddForeignKey(
                name: "FK_AppCourseSessions_AppCourses_CourseId",
                table: "AppCourseSessions",
                column: "CourseId",
                principalTable: "AppCourses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
