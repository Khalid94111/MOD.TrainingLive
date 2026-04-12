using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class updateTrainingBudget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrnTrainingBudgets_TenantId_Year_BudgetType",
                table: "TrnTrainingBudgets");

            migrationBuilder.DropColumn(
                name: "BudgetType",
                table: "TrnTrainingBudgets");

            migrationBuilder.AddColumn<Guid>(
                name: "FinancialItemId",
                table: "TrnTrainingBudgets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_TrnTrainingBudgets_FinancialItemId",
                table: "TrnTrainingBudgets",
                column: "FinancialItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TrnTrainingBudgets_TenantId_Year_FinancialItemId",
                table: "TrnTrainingBudgets",
                columns: new[] { "TenantId", "Year", "FinancialItemId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_TrnTrainingBudgets_TrnFinancialItems_FinancialItemId",
                table: "TrnTrainingBudgets",
                column: "FinancialItemId",
                principalTable: "TrnFinancialItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrnTrainingBudgets_TrnFinancialItems_FinancialItemId",
                table: "TrnTrainingBudgets");

            migrationBuilder.DropIndex(
                name: "IX_TrnTrainingBudgets_FinancialItemId",
                table: "TrnTrainingBudgets");

            migrationBuilder.DropIndex(
                name: "IX_TrnTrainingBudgets_TenantId_Year_FinancialItemId",
                table: "TrnTrainingBudgets");

            migrationBuilder.DropColumn(
                name: "FinancialItemId",
                table: "TrnTrainingBudgets");

            migrationBuilder.AddColumn<int>(
                name: "BudgetType",
                table: "TrnTrainingBudgets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TrnTrainingBudgets_TenantId_Year_BudgetType",
                table: "TrnTrainingBudgets",
                columns: new[] { "TenantId", "Year", "BudgetType" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }
    }
}
