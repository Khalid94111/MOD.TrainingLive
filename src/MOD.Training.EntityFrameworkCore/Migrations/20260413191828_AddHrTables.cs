using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class AddHrTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HrRanks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    PersonnelType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrRanks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HrEmployees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MainUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FullNameAr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FullNameEn = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JoinDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Education = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Nationality = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MedicalFitness = table.Column<bool>(type: "bit", nullable: false),
                    SecurityClearance = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LanguageLevel = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrEmployees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HrEmployees_HrRanks_RankId",
                        column: x => x.RankId,
                        principalTable: "HrRanks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_HrEmployees_MainUnitId",
                table: "HrEmployees",
                column: "MainUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_HrEmployees_RankId",
                table: "HrEmployees",
                column: "RankId");

            migrationBuilder.CreateIndex(
                name: "IX_HrEmployees_ServiceNumber",
                table: "HrEmployees",
                column: "ServiceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_HrEmployees_UserId",
                table: "HrEmployees",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HrEmployees");

            migrationBuilder.DropTable(
                name: "HrRanks");
        }
    }
}
