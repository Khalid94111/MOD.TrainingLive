using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class AddClothingAllowanceRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClothingAllowanceRuleId",
                table: "TrvEmployeeClothingHistories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TrvClothingAllowanceRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FullAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualPartialAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FullPaymentPeriodYears = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_TrvClothingAllowanceRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrvClothingAllowanceRuleRanks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClothingAllowanceRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrvClothingAllowanceRuleRanks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrvClothingAllowanceRuleRanks_TrvClothingAllowanceRules_ClothingAllowanceRuleId",
                        column: x => x.ClothingAllowanceRuleId,
                        principalTable: "TrvClothingAllowanceRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrvEmployeeClothingHistories_ClothingAllowanceRuleId",
                table: "TrvEmployeeClothingHistories",
                column: "ClothingAllowanceRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvClothingAllowanceRuleRanks_ClothingAllowanceRuleId",
                table: "TrvClothingAllowanceRuleRanks",
                column: "ClothingAllowanceRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvClothingAllowanceRuleRanks_ClothingAllowanceRuleId_RankId",
                table: "TrvClothingAllowanceRuleRanks",
                columns: new[] { "ClothingAllowanceRuleId", "RankId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrvClothingAllowanceRuleRanks_RankId",
                table: "TrvClothingAllowanceRuleRanks",
                column: "RankId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvClothingAllowanceRules_IsActive",
                table: "TrvClothingAllowanceRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TrvClothingAllowanceRules_Priority",
                table: "TrvClothingAllowanceRules",
                column: "Priority");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrvClothingAllowanceRuleRanks");

            migrationBuilder.DropTable(
                name: "TrvClothingAllowanceRules");

            migrationBuilder.DropIndex(
                name: "IX_TrvEmployeeClothingHistories_ClothingAllowanceRuleId",
                table: "TrvEmployeeClothingHistories");

            migrationBuilder.DropColumn(
                name: "ClothingAllowanceRuleId",
                table: "TrvEmployeeClothingHistories");
        }
    }
}
