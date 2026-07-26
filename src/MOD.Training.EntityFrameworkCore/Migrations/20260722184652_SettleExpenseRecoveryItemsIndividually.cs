using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class SettleExpenseRecoveryItemsIndividually : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSettled",
                table: "AppTrainingExpenseRecoveryItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SettledAt",
                table: "AppTrainingExpenseRecoveryItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SettledById",
                table: "AppTrainingExpenseRecoveryItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SettlementNote",
                table: "AppTrainingExpenseRecoveryItems",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SettlementReference",
                table: "AppTrainingExpenseRecoveryItems",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE item
                SET item.IsSettled = 1,
                    item.SettledAt = recovery.SettledAt,
                    item.SettledById = recovery.SettledById,
                    item.SettlementReference = recovery.SettlementReference,
                    item.SettlementNote = recovery.SettlementNote
                FROM AppTrainingExpenseRecoveryItems AS item
                INNER JOIN AppTrainingExpenseRecoveries AS recovery
                    ON recovery.Id = item.TrainingExpenseRecoveryId
                WHERE recovery.Status = 2;

                UPDATE AppTrainingExpenseRecoveries
                SET Status = 3
                WHERE Status = 2;
                """);

            migrationBuilder.DropColumn(
                name: "SettledAt",
                table: "AppTrainingExpenseRecoveries");

            migrationBuilder.DropColumn(
                name: "SettledById",
                table: "AppTrainingExpenseRecoveries");

            migrationBuilder.DropColumn(
                name: "SettlementNote",
                table: "AppTrainingExpenseRecoveries");

            migrationBuilder.DropColumn(
                name: "SettlementReference",
                table: "AppTrainingExpenseRecoveries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SettledAt",
                table: "AppTrainingExpenseRecoveries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SettledById",
                table: "AppTrainingExpenseRecoveries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SettlementNote",
                table: "AppTrainingExpenseRecoveries",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SettlementReference",
                table: "AppTrainingExpenseRecoveries",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE recovery
                SET recovery.SettledAt = latest.SettledAt,
                    recovery.SettledById = latest.SettledById,
                    recovery.SettlementReference = latest.SettlementReference,
                    recovery.SettlementNote = latest.SettlementNote,
                    recovery.Status = CASE WHEN recovery.Status = 3 THEN 2 ELSE 1 END
                FROM AppTrainingExpenseRecoveries AS recovery
                OUTER APPLY (
                    SELECT TOP (1)
                        item.SettledAt,
                        item.SettledById,
                        item.SettlementReference,
                        item.SettlementNote
                    FROM AppTrainingExpenseRecoveryItems AS item
                    WHERE item.TrainingExpenseRecoveryId = recovery.Id
                      AND item.IsSettled = 1
                    ORDER BY item.SettledAt DESC
                ) AS latest
                WHERE recovery.Status IN (2, 3);
                """);

            migrationBuilder.DropColumn(
                name: "IsSettled",
                table: "AppTrainingExpenseRecoveryItems");

            migrationBuilder.DropColumn(
                name: "SettledAt",
                table: "AppTrainingExpenseRecoveryItems");

            migrationBuilder.DropColumn(
                name: "SettledById",
                table: "AppTrainingExpenseRecoveryItems");

            migrationBuilder.DropColumn(
                name: "SettlementNote",
                table: "AppTrainingExpenseRecoveryItems");

            migrationBuilder.DropColumn(
                name: "SettlementReference",
                table: "AppTrainingExpenseRecoveryItems");
        }
    }
}
