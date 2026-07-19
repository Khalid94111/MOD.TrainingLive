using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class LinkCenterPlanItemsAndReservedSeats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TrainingCenterPlans",
                table: "TrainingCenterPlans");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrainingCenterPlanItemUnits",
                table: "TrainingCenterPlanItemUnits");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrainingCenterPlanItems",
                table: "TrainingCenterPlanItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CenterRoleAssignments",
                table: "CenterRoleAssignments");

            migrationBuilder.RenameTable(
                name: "TrainingCenterPlans",
                newName: "TrnTrainingCenterPlans");

            migrationBuilder.RenameTable(
                name: "TrainingCenterPlanItemUnits",
                newName: "TrnTrainingCenterPlanItemUnits");

            migrationBuilder.RenameTable(
                name: "TrainingCenterPlanItems",
                newName: "TrnTrainingCenterPlanItems");

            migrationBuilder.RenameTable(
                name: "CenterRoleAssignments",
                newName: "TrnCenterRoleAssignments");

            migrationBuilder.AddColumn<Guid>(
                name: "TrainingCenterPlanItemId",
                table: "AppTrainingPlanItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TrnTrainingCenterPlans",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Objective",
                table: "TrnTrainingCenterPlanItems",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BeneficiaryType",
                table: "TrnTrainingCenterPlanItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ReservedSeats",
                table: "TrnTrainingCenterPlanItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TrnTrainingCenterPlanItems",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<string>(
                name: "ServiceNumber",
                table: "TrnCenterRoleAssignments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RoleType",
                table: "TrnCenterRoleAssignments",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "AssignmentType",
                table: "TrnCenterRoleAssignments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrnTrainingCenterPlans",
                table: "TrnTrainingCenterPlans",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrnTrainingCenterPlanItemUnits",
                table: "TrnTrainingCenterPlanItemUnits",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrnTrainingCenterPlanItems",
                table: "TrnTrainingCenterPlanItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrnCenterRoleAssignments",
                table: "TrnCenterRoleAssignments",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_AppTrainingPlanItems_PlanId_TrainingCenterPlanItemId_UnitId",
                table: "AppTrainingPlanItems",
                columns: new[] { "PlanId", "TrainingCenterPlanItemId", "UnitId" },
                unique: true,
                filter: "[TrainingCenterPlanItemId] IS NOT NULL AND [UnitId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppTrainingPlanItems_TrainingCenterPlanItemId",
                table: "AppTrainingPlanItems",
                column: "TrainingCenterPlanItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TrnTrainingCenterPlans_CenterId",
                table: "TrnTrainingCenterPlans",
                column: "CenterId");

            migrationBuilder.CreateIndex(
                name: "IX_TrnTrainingCenterPlanItemUnits_PlanItemId_UnitId",
                table: "TrnTrainingCenterPlanItemUnits",
                columns: new[] { "PlanItemId", "UnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrnTrainingCenterPlanItems_PlanId",
                table: "TrnTrainingCenterPlanItems",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TrnCenterRoleAssignments_CenterId",
                table: "TrnCenterRoleAssignments",
                column: "CenterId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppTrainingPlanItems_TrnTrainingCenterPlanItems_TrainingCenterPlanItemId",
                table: "AppTrainingPlanItems",
                column: "TrainingCenterPlanItemId",
                principalTable: "TrnTrainingCenterPlanItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrnCenterRoleAssignments_TrainingCenters_CenterId",
                table: "TrnCenterRoleAssignments",
                column: "CenterId",
                principalTable: "TrainingCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrnTrainingCenterPlanItems_TrnTrainingCenterPlans_PlanId",
                table: "TrnTrainingCenterPlanItems",
                column: "PlanId",
                principalTable: "TrnTrainingCenterPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrnTrainingCenterPlanItemUnits_TrnTrainingCenterPlanItems_PlanItemId",
                table: "TrnTrainingCenterPlanItemUnits",
                column: "PlanItemId",
                principalTable: "TrnTrainingCenterPlanItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrnTrainingCenterPlans_TrainingCenters_CenterId",
                table: "TrnTrainingCenterPlans",
                column: "CenterId",
                principalTable: "TrainingCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppTrainingPlanItems_TrnTrainingCenterPlanItems_TrainingCenterPlanItemId",
                table: "AppTrainingPlanItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TrnCenterRoleAssignments_TrainingCenters_CenterId",
                table: "TrnCenterRoleAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_TrnTrainingCenterPlanItems_TrnTrainingCenterPlans_PlanId",
                table: "TrnTrainingCenterPlanItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TrnTrainingCenterPlanItemUnits_TrnTrainingCenterPlanItems_PlanItemId",
                table: "TrnTrainingCenterPlanItemUnits");

            migrationBuilder.DropForeignKey(
                name: "FK_TrnTrainingCenterPlans_TrainingCenters_CenterId",
                table: "TrnTrainingCenterPlans");

            migrationBuilder.DropIndex(
                name: "IX_AppTrainingPlanItems_PlanId_TrainingCenterPlanItemId_UnitId",
                table: "AppTrainingPlanItems");

            migrationBuilder.DropIndex(
                name: "IX_AppTrainingPlanItems_TrainingCenterPlanItemId",
                table: "AppTrainingPlanItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrnTrainingCenterPlans",
                table: "TrnTrainingCenterPlans");

            migrationBuilder.DropIndex(
                name: "IX_TrnTrainingCenterPlans_CenterId",
                table: "TrnTrainingCenterPlans");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrnTrainingCenterPlanItemUnits",
                table: "TrnTrainingCenterPlanItemUnits");

            migrationBuilder.DropIndex(
                name: "IX_TrnTrainingCenterPlanItemUnits_PlanItemId_UnitId",
                table: "TrnTrainingCenterPlanItemUnits");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrnTrainingCenterPlanItems",
                table: "TrnTrainingCenterPlanItems");

            migrationBuilder.DropIndex(
                name: "IX_TrnTrainingCenterPlanItems_PlanId",
                table: "TrnTrainingCenterPlanItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrnCenterRoleAssignments",
                table: "TrnCenterRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_TrnCenterRoleAssignments_CenterId",
                table: "TrnCenterRoleAssignments");

            migrationBuilder.DropColumn(
                name: "TrainingCenterPlanItemId",
                table: "AppTrainingPlanItems");

            migrationBuilder.DropColumn(
                name: "ReservedSeats",
                table: "TrnTrainingCenterPlanItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TrnTrainingCenterPlanItems");

            migrationBuilder.RenameTable(
                name: "TrnTrainingCenterPlans",
                newName: "TrainingCenterPlans");

            migrationBuilder.RenameTable(
                name: "TrnTrainingCenterPlanItemUnits",
                newName: "TrainingCenterPlanItemUnits");

            migrationBuilder.RenameTable(
                name: "TrnTrainingCenterPlanItems",
                newName: "TrainingCenterPlanItems");

            migrationBuilder.RenameTable(
                name: "TrnCenterRoleAssignments",
                newName: "CenterRoleAssignments");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "TrainingCenterPlans",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Objective",
                table: "TrainingCenterPlanItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BeneficiaryType",
                table: "TrainingCenterPlanItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "ServiceNumber",
                table: "CenterRoleAssignments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RoleType",
                table: "CenterRoleAssignments",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<int>(
                name: "AssignmentType",
                table: "CenterRoleAssignments",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrainingCenterPlans",
                table: "TrainingCenterPlans",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrainingCenterPlanItemUnits",
                table: "TrainingCenterPlanItemUnits",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrainingCenterPlanItems",
                table: "TrainingCenterPlanItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CenterRoleAssignments",
                table: "CenterRoleAssignments",
                column: "Id");
        }
    }
}
