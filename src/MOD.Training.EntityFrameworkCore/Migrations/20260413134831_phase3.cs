using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class phase3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppCourses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CourseType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AppCourses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppPlanItemFinancialItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlanItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstimatedAmountOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    EstimatedAmountUSD = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ActualAmountOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    ActualAmountUSD = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_AppPlanItemFinancialItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppTrainingPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OpenDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CloseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
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
                    table.PrimaryKey("PK_AppTrainingPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppTrainingProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProviderNameAr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProviderNameEn = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AverageRating = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    TotalRatings = table.Column<int>(type: "int", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_AppTrainingProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppCourseSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MaxSeats = table.Column<int>(type: "int", nullable: false),
                    AvailableSeats = table.Column<int>(type: "int", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompletionStatus = table.Column<int>(type: "int", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_AppCourseSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppCourseSessions_AppCourses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "AppCourses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppTrainingPlanItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseType = table.Column<int>(type: "int", nullable: false),
                    PreferredQuarter = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    OfficersCount = table.Column<int>(type: "int", nullable: false),
                    EnlistedCount = table.Column<int>(type: "int", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ObjectivesAr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ObjectivesEn = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DurationYears = table.Column<int>(type: "int", nullable: false),
                    DurationMonths = table.Column<int>(type: "int", nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    EstimatedDateFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedDateTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    FundingSource = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SubmittedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_AppTrainingPlanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppTrainingPlanItems_AppTrainingPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "AppTrainingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppNominations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NominatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    NominatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttendanceStatus = table.Column<int>(type: "int", nullable: true),
                    ResultType = table.Column<int>(type: "int", nullable: true),
                    ResultValue = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResultEnteredById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResultEnteredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_AppNominations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppNominations_AppCourseSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AppCourseSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppPriceQuotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PricingType = table.Column<int>(type: "int", nullable: false),
                    QuotedPrice = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    PricePerPerson = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    ParticipantsCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_AppPriceQuotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppPriceQuotes_AppCourseSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AppCourseSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppPriceQuotes_AppTrainingProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "AppTrainingProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppSessionConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConditionType = table.Column<int>(type: "int", nullable: false),
                    ConditionValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSessionConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSessionConditions_AppCourseSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AppCourseSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppPlanItemConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConditionType = table.Column<int>(type: "int", nullable: false),
                    ConditionValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPlanItemConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppPlanItemConditions_AppTrainingPlanItems_PlanItemId",
                        column: x => x.PlanItemId,
                        principalTable: "AppTrainingPlanItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppNominationApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NominationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalLevel = table.Column<int>(type: "int", nullable: false),
                    ApprovedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppNominationApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppNominationApprovals_AppNominations_NominationId",
                        column: x => x.NominationId,
                        principalTable: "AppNominations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppCourses_TenantCourseId",
                table: "AppCourses",
                column: "TenantCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCourseSessions_CourseId",
                table: "AppCourseSessions",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCourseSessions_SessionCode",
                table: "AppCourseSessions",
                column: "SessionCode");

            migrationBuilder.CreateIndex(
                name: "IX_AppNominationApprovals_NominationId",
                table: "AppNominationApprovals",
                column: "NominationId");

            migrationBuilder.CreateIndex(
                name: "IX_AppNominationApprovals_NominationId_ApprovalLevel",
                table: "AppNominationApprovals",
                columns: new[] { "NominationId", "ApprovalLevel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppNominations_EmployeeId",
                table: "AppNominations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AppNominations_SessionId",
                table: "AppNominations",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppNominations_SessionId_EmployeeId",
                table: "AppNominations",
                columns: new[] { "SessionId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppPlanItemConditions_PlanItemId",
                table: "AppPlanItemConditions",
                column: "PlanItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPlanItemFinancialItems_PlanItemId",
                table: "AppPlanItemFinancialItems",
                column: "PlanItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPlanItemFinancialItems_PlanItemId_FinancialItemId",
                table: "AppPlanItemFinancialItems",
                columns: new[] { "PlanItemId", "FinancialItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppPriceQuotes_ProviderId",
                table: "AppPriceQuotes",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPriceQuotes_SessionId",
                table: "AppPriceQuotes",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSessionConditions_SessionId",
                table: "AppSessionConditions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTrainingPlanItems_PlanId",
                table: "AppTrainingPlanItems",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTrainingPlanItems_TenantCourseId",
                table: "AppTrainingPlanItems",
                column: "TenantCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTrainingPlans_TenantId_Year",
                table: "AppTrainingPlans",
                columns: new[] { "TenantId", "Year" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppNominationApprovals");

            migrationBuilder.DropTable(
                name: "AppPlanItemConditions");

            migrationBuilder.DropTable(
                name: "AppPlanItemFinancialItems");

            migrationBuilder.DropTable(
                name: "AppPriceQuotes");

            migrationBuilder.DropTable(
                name: "AppSessionConditions");

            migrationBuilder.DropTable(
                name: "AppNominations");

            migrationBuilder.DropTable(
                name: "AppTrainingPlanItems");

            migrationBuilder.DropTable(
                name: "AppTrainingProviders");

            migrationBuilder.DropTable(
                name: "AppCourseSessions");

            migrationBuilder.DropTable(
                name: "AppTrainingPlans");

            migrationBuilder.DropTable(
                name: "AppCourses");
        }
    }
}
