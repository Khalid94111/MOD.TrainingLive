using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelRequestAllowanceDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrvTravelRequestAllowanceDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravelRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RankName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    DailyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CalculatedDays = table.Column<int>(type: "int", nullable: false),
                    AccommodationPaymentPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OverseasTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClothingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeductionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HasMatchingRule = table.Column<bool>(type: "bit", nullable: false),
                    SegmentsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    ClothingCalculationNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrvTravelRequestAllowanceDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrvTravelRequestAllowanceDetails_TrvTravelRequests_TravelRequestId",
                        column: x => x.TravelRequestId,
                        principalTable: "TrvTravelRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequestAllowanceDetails_CalculatedAt",
                table: "TrvTravelRequestAllowanceDetails",
                column: "CalculatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequestAllowanceDetails_Category",
                table: "TrvTravelRequestAllowanceDetails",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequestAllowanceDetails_EmployeeId",
                table: "TrvTravelRequestAllowanceDetails",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequestAllowanceDetails_EmployeeNumber",
                table: "TrvTravelRequestAllowanceDetails",
                column: "EmployeeNumber");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequestAllowanceDetails_TravelRequestId",
                table: "TrvTravelRequestAllowanceDetails",
                column: "TravelRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrvTravelRequestAllowanceDetails");
        }
    }
}
