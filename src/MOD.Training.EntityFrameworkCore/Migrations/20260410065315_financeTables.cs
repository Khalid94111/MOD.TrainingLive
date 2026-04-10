using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class financeTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrnExchangeRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ToCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SetById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SetAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrnExchangeRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrnFinancialItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VoteCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsGeneral = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_TrnFinancialItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrnFinancialItems_TrnFinancialItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "TrnFinancialItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrnTrainingBudgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Year = table.Column<int>(type: "int", nullable: false),
                    BudgetType = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    SpentAmount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    AlertThreshold = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_TrnTrainingBudgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrnCourseTypeFinancialItemDefaults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CourseType = table.Column<int>(type: "int", nullable: false),
                    FinancialItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrnCourseTypeFinancialItemDefaults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrnCourseTypeFinancialItemDefaults_TrnFinancialItems_FinancialItemId",
                        column: x => x.FinancialItemId,
                        principalTable: "TrnFinancialItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrnCourseTypeFinancialItemDefaults_FinancialItemId",
                table: "TrnCourseTypeFinancialItemDefaults",
                column: "FinancialItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TrnCourseTypeFinancialItemDefaults_TenantId_CourseType_FinancialItemId",
                table: "TrnCourseTypeFinancialItemDefaults",
                columns: new[] { "TenantId", "CourseType", "FinancialItemId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TrnExchangeRates_IsActive",
                table: "TrnExchangeRates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TrnFinancialItems_ParentId",
                table: "TrnFinancialItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_TrnFinancialItems_TenantId_ParentId",
                table: "TrnFinancialItems",
                columns: new[] { "TenantId", "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_TrnTrainingBudgets_TenantId_Year_BudgetType",
                table: "TrnTrainingBudgets",
                columns: new[] { "TenantId", "Year", "BudgetType" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrnCourseTypeFinancialItemDefaults");

            migrationBuilder.DropTable(
                name: "TrnExchangeRates");

            migrationBuilder.DropTable(
                name: "TrnTrainingBudgets");

            migrationBuilder.DropTable(
                name: "TrnFinancialItems");
        }
    }
}
