using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class LinkAccommodationRulesToAllowanceRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrvAccommodationRuleAllowanceRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccommodationRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllowanceRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrvAccommodationRuleAllowanceRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrvAccommodationRuleAllowanceRules_AccommodationRuleId",
                table: "TrvAccommodationRuleAllowanceRules",
                column: "AccommodationRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAccommodationRuleAllowanceRules_AccommodationRuleId_AllowanceRuleId",
                table: "TrvAccommodationRuleAllowanceRules",
                columns: new[] { "AccommodationRuleId", "AllowanceRuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrvAccommodationRuleAllowanceRules_AllowanceRuleId",
                table: "TrvAccommodationRuleAllowanceRules",
                column: "AllowanceRuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrvAccommodationRuleAllowanceRules");
        }
    }
}
