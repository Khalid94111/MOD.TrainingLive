using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class Phase4ACasualCourses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppCasualCourses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseType = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ObjectivesAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DurationYears = table.Column<int>(type: "int", nullable: false),
                    DurationMonths = table.Column<int>(type: "int", nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    EstimatedDateFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstimatedDateTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FundingSource = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EstimatedTotalCost = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    FundingScenario = table.Column<int>(type: "int", nullable: true),
                    SelectedPriceQuoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReturnedFromStatus = table.Column<int>(type: "int", nullable: true),
                    IsReturned = table.Column<bool>(type: "bit", nullable: false),
                    LastReturnNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_AppCasualCourses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppCasualCourseFinancials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CasualCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstimatedAmountOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    ActualAmountOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_AppCasualCourseFinancials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppCasualCourseFinancials_AppCasualCourses_CasualCourseId",
                        column: x => x.CasualCourseId,
                        principalTable: "AppCasualCourses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppCasualCourseNominations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CasualCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsReturned = table.Column<bool>(type: "bit", nullable: false),
                    LastReturnNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConditionSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_AppCasualCourseNominations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppCasualCourseNominations_AppCasualCourses_CasualCourseId",
                        column: x => x.CasualCourseId,
                        principalTable: "AppCasualCourses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppCasualCourseFinancials_CasualCourseId_FinancialItemId",
                table: "AppCasualCourseFinancials",
                columns: new[] { "CasualCourseId", "FinancialItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppCasualCourseNominations_CasualCourseId_EmployeeId",
                table: "AppCasualCourseNominations",
                columns: new[] { "CasualCourseId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppCasualCourses_TenantId_Status",
                table: "AppCasualCourses",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppCasualCourses_TenantId_UnitId",
                table: "AppCasualCourses",
                columns: new[] { "TenantId", "UnitId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppCasualCourseFinancials");

            migrationBuilder.DropTable(
                name: "AppCasualCourseNominations");

            migrationBuilder.DropTable(
                name: "AppCasualCourses");
        }
    }
}
