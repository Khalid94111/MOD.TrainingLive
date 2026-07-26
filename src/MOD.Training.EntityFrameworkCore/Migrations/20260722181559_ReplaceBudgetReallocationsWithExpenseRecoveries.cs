using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceBudgetReallocationsWithExpenseRecoveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppBudgetReallocations");

            migrationBuilder.CreateTable(
                name: "AppTrainingExpenseRecoveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CasualCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravelRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    TotalAmountOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SettledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SettledById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SettlementReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SettlementNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_AppTrainingExpenseRecoveries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppTrainingExpenseRecoveryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TrainingExpenseRecoveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpenseTypeCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FundingSourceVoteCode = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AmountOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
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
                    table.PrimaryKey("PK_AppTrainingExpenseRecoveryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppTrainingExpenseRecoveryItems_AppTrainingExpenseRecoveries_TrainingExpenseRecoveryId",
                        column: x => x.TrainingExpenseRecoveryId,
                        principalTable: "AppTrainingExpenseRecoveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppTrainingExpenseRecoveries_TenantId_CasualCourseId",
                table: "AppTrainingExpenseRecoveries",
                columns: new[] { "TenantId", "CasualCourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppTrainingExpenseRecoveries_TenantId_CasualCourseId_TravelRequestId",
                table: "AppTrainingExpenseRecoveries",
                columns: new[] { "TenantId", "CasualCourseId", "TravelRequestId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppTrainingExpenseRecoveries_TenantId_Status",
                table: "AppTrainingExpenseRecoveries",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppTrainingExpenseRecoveryItems_TenantId_FinancialItemId",
                table: "AppTrainingExpenseRecoveryItems",
                columns: new[] { "TenantId", "FinancialItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppTrainingExpenseRecoveryItems_TenantId_TrainingExpenseRecoveryId",
                table: "AppTrainingExpenseRecoveryItems",
                columns: new[] { "TenantId", "TrainingExpenseRecoveryId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppTrainingExpenseRecoveryItems_TrainingExpenseRecoveryId_ExpenseTypeCode_FundingSourceVoteCode",
                table: "AppTrainingExpenseRecoveryItems",
                columns: new[] { "TrainingExpenseRecoveryId", "ExpenseTypeCode", "FundingSourceVoteCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppTrainingExpenseRecoveryItems");

            migrationBuilder.DropTable(
                name: "AppTrainingExpenseRecoveries");

            migrationBuilder.CreateTable(
                name: "AppBudgetReallocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmountOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    ApprovalNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CasualCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CoursePaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FundingSourceVoteCode = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToFinancialItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppBudgetReallocations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppBudgetReallocations_TenantId_CasualCourseId",
                table: "AppBudgetReallocations",
                columns: new[] { "TenantId", "CasualCourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppBudgetReallocations_TenantId_CoursePaymentId",
                table: "AppBudgetReallocations",
                columns: new[] { "TenantId", "CoursePaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppBudgetReallocations_TenantId_Status",
                table: "AppBudgetReallocations",
                columns: new[] { "TenantId", "Status" });
        }
    }
}
