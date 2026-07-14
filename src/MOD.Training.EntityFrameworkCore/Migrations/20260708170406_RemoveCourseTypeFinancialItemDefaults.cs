using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCourseTypeFinancialItemDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrnCourseTypeFinancialItemDefaults");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrnCourseTypeFinancialItemDefaults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseType = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
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
        }
    }
}
