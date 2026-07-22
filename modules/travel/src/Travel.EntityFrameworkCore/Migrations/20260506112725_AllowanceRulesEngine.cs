using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class AllowanceRulesEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RequesterName",
                table: "TrvTravelRequests",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<Guid>(
                name: "RankId",
                table: "TrvTravelRequestEmployees",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RankName",
                table: "TrvTravelRequestEmployees",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "TrvAllowanceRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllowanceType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_TrvAllowanceRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrvAllowanceRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AllowanceType = table.Column<int>(type: "int", nullable: false),
                    TravelType = table.Column<int>(type: "int", nullable: true),
                    RankId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccommodationMultiplier = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
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
                    table.PrimaryKey("PK_TrvAllowanceRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrvEmployeeClothingHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravelRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsFullPayment = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_TrvEmployeeClothingHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrvAllowanceRuleSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllowanceRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromDay = table.Column<int>(type: "int", nullable: false),
                    ToDay = table.Column<int>(type: "int", nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrvAllowanceRuleSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrvAllowanceRuleSegments_TrvAllowanceRules_AllowanceRuleId",
                        column: x => x.AllowanceRuleId,
                        principalTable: "TrvAllowanceRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRates_AllowanceType",
                table: "TrvAllowanceRates",
                column: "AllowanceType");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRates_IsActive",
                table: "TrvAllowanceRates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRates_RankId",
                table: "TrvAllowanceRates",
                column: "RankId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRules_AllowanceType",
                table: "TrvAllowanceRules",
                column: "AllowanceType");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRules_IsActive",
                table: "TrvAllowanceRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRules_Priority",
                table: "TrvAllowanceRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRules_RankId",
                table: "TrvAllowanceRules",
                column: "RankId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRules_TravelType",
                table: "TrvAllowanceRules",
                column: "TravelType");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRuleSegments_AllowanceRuleId",
                table: "TrvAllowanceRuleSegments",
                column: "AllowanceRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvEmployeeClothingHistories_EmployeeId",
                table: "TrvEmployeeClothingHistories",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvEmployeeClothingHistories_TravelRequestId",
                table: "TrvEmployeeClothingHistories",
                column: "TravelRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrvAllowanceRates");

            migrationBuilder.DropTable(
                name: "TrvAllowanceRuleSegments");

            migrationBuilder.DropTable(
                name: "TrvEmployeeClothingHistories");

            migrationBuilder.DropTable(
                name: "TrvAllowanceRules");

            migrationBuilder.DropColumn(
                name: "RankId",
                table: "TrvTravelRequestEmployees");

            migrationBuilder.DropColumn(
                name: "RankName",
                table: "TrvTravelRequestEmployees");

            migrationBuilder.AlterColumn<string>(
                name: "RequesterName",
                table: "TrvTravelRequests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);
        }
    }
}
