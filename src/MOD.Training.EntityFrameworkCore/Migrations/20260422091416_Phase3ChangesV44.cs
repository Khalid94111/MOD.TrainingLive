using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class Phase3ChangesV44 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppBooks");

            migrationBuilder.DropTable(
                name: "AppOranges");

            migrationBuilder.DropIndex(
                name: "IX_AppNominations_SessionId_EmployeeId",
                table: "AppNominations");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "AppTrainingPlanItems");

            migrationBuilder.DropColumn(
                name: "EnlistedCount",
                table: "AppTrainingPlanItems");

            migrationBuilder.DropColumn(
                name: "OfficersCount",
                table: "AppTrainingPlanItems");

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultAmountOMR",
                table: "TrnFinancialItems",
                type: "decimal(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ExtraDaysAfter",
                table: "TrnFinancialItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExtraDaysBefore",
                table: "TrnFinancialItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPerDay",
                table: "TrnFinancialItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPerNominee",
                table: "TrnFinancialItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsReturned",
                table: "AppTrainingPlanItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LastReturnNoteId",
                table: "AppTrainingPlanItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SessionId",
                table: "AppNominations",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<bool>(
                name: "IsReturned",
                table: "AppNominations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LastReturnNoteId",
                table: "AppNominations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlanItemId",
                table: "AppNominations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "AppFinancialItemRankAmounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FinancialItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_AppFinancialItemRankAmounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppFinancialItemRankAmounts_TrnFinancialItems_FinancialItemId",
                        column: x => x.FinancialItemId,
                        principalTable: "TrnFinancialItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppPlanItemFinancialItemRanks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlanItemFinancialItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomineeCount = table.Column<int>(type: "int", nullable: false),
                    RatePerUnitOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    SubtotalOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
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
                    table.PrimaryKey("PK_AppPlanItemFinancialItemRanks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppPlanItemFinancialItemRanks_AppPlanItemFinancialItems_PlanItemFinancialItemId",
                        column: x => x.PlanItemFinancialItemId,
                        principalTable: "AppPlanItemFinancialItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppPlanNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AuthorRole = table.Column<int>(type: "int", nullable: false),
                    IsReturnReason = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPlanNotes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppNominations_PlanItemId",
                table: "AppNominations",
                column: "PlanItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AppNominations_PlanItemId_EmployeeId",
                table: "AppNominations",
                columns: new[] { "PlanItemId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppFinancialItemRankAmounts_FinancialItemId",
                table: "AppFinancialItemRankAmounts",
                column: "FinancialItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AppFinancialItemRankAmounts_TenantId_FinancialItemId_RankId",
                table: "AppFinancialItemRankAmounts",
                columns: new[] { "TenantId", "FinancialItemId", "RankId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppPlanItemFinancialItemRanks_PlanItemFinancialItemId",
                table: "AppPlanItemFinancialItemRanks",
                column: "PlanItemFinancialItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPlanItemFinancialItemRanks_TenantId_PlanItemFinancialItemId_RankId",
                table: "AppPlanItemFinancialItemRanks",
                columns: new[] { "TenantId", "PlanItemFinancialItemId", "RankId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppPlanNotes_TenantId_EntityType_EntityId_CreationTime",
                table: "AppPlanNotes",
                columns: new[] { "TenantId", "EntityType", "EntityId", "CreationTime" });

            migrationBuilder.AddForeignKey(
                name: "FK_AppNominations_AppTrainingPlanItems_PlanItemId",
                table: "AppNominations",
                column: "PlanItemId",
                principalTable: "AppTrainingPlanItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppNominations_AppTrainingPlanItems_PlanItemId",
                table: "AppNominations");

            migrationBuilder.DropTable(
                name: "AppFinancialItemRankAmounts");

            migrationBuilder.DropTable(
                name: "AppPlanItemFinancialItemRanks");

            migrationBuilder.DropTable(
                name: "AppPlanNotes");

            migrationBuilder.DropIndex(
                name: "IX_AppNominations_PlanItemId",
                table: "AppNominations");

            migrationBuilder.DropIndex(
                name: "IX_AppNominations_PlanItemId_EmployeeId",
                table: "AppNominations");

            migrationBuilder.DropColumn(
                name: "DefaultAmountOMR",
                table: "TrnFinancialItems");

            migrationBuilder.DropColumn(
                name: "ExtraDaysAfter",
                table: "TrnFinancialItems");

            migrationBuilder.DropColumn(
                name: "ExtraDaysBefore",
                table: "TrnFinancialItems");

            migrationBuilder.DropColumn(
                name: "IsPerDay",
                table: "TrnFinancialItems");

            migrationBuilder.DropColumn(
                name: "IsPerNominee",
                table: "TrnFinancialItems");

            migrationBuilder.DropColumn(
                name: "IsReturned",
                table: "AppTrainingPlanItems");

            migrationBuilder.DropColumn(
                name: "LastReturnNoteId",
                table: "AppTrainingPlanItems");

            migrationBuilder.DropColumn(
                name: "IsReturned",
                table: "AppNominations");

            migrationBuilder.DropColumn(
                name: "LastReturnNoteId",
                table: "AppNominations");

            migrationBuilder.DropColumn(
                name: "PlanItemId",
                table: "AppNominations");

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "AppTrainingPlanItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EnlistedCount",
                table: "AppTrainingPlanItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OfficersCount",
                table: "AppTrainingPlanItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "SessionId",
                table: "AppNominations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "AppBooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Price = table.Column<float>(type: "real", nullable: false),
                    PublishDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppBooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppOranges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArabicName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppOranges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppNominations_SessionId_EmployeeId",
                table: "AppNominations",
                columns: new[] { "SessionId", "EmployeeId" },
                unique: true);
        }
    }
}
