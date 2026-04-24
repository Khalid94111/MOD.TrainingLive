using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class v4_5_2_CasualCourseFinancialItemRanks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppCasualCourseFinancialItemRanks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CasualCourseFinancialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomineeCount = table.Column<int>(type: "int", nullable: false),
                    RatePerUnitOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    SubtotalOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    RateSource = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_AppCasualCourseFinancialItemRanks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppCasualCourseFinancialItemRanks_AppCasualCourseFinancials_CasualCourseFinancialId",
                        column: x => x.CasualCourseFinancialId,
                        principalTable: "AppCasualCourseFinancials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppCasualCourseFinancialItemRanks_CasualCourseFinancialId",
                table: "AppCasualCourseFinancialItemRanks",
                column: "CasualCourseFinancialId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCasualCourseFinancialItemRanks_TenantId_CasualCourseFinancialId_RankId",
                table: "AppCasualCourseFinancialItemRanks",
                columns: new[] { "TenantId", "CasualCourseFinancialId", "RankId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppCasualCourseFinancialItemRanks");
        }
    }
}
