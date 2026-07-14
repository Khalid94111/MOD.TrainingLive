using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReplaceNominationLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppNominations_PlanItemId_EmployeeId",
                table: "AppNominations");

            migrationBuilder.DropColumn(
                name: "ReplacedNominationId",
                table: "AppNominations");

            migrationBuilder.CreateIndex(
                name: "IX_AppNominations_PlanItemId_EmployeeId",
                table: "AppNominations",
                columns: new[] { "PlanItemId", "EmployeeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppNominations_PlanItemId_EmployeeId",
                table: "AppNominations");

            migrationBuilder.AddColumn<Guid>(
                name: "ReplacedNominationId",
                table: "AppNominations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppNominations_PlanItemId_EmployeeId",
                table: "AppNominations",
                columns: new[] { "PlanItemId", "EmployeeId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
