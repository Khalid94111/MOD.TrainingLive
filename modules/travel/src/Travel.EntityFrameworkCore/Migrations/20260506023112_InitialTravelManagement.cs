using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class InitialTravelManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrvTravelRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DestinationCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DestinationCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DailyAllowanceRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    NeedsPermission = table.Column<bool>(type: "bit", nullable: false),
                    IncludesAccommodation = table.Column<bool>(type: "bit", nullable: false),
                    RequesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllowanceSnapshot_OverseasTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AllowanceSnapshot_ClothingTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AllowanceSnapshot_DeductionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AllowanceSnapshot_CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_TrvTravelRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrvTravelDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravelRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    BlobName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrvTravelDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrvTravelDocuments_TrvTravelRequests_TravelRequestId",
                        column: x => x.TravelRequestId,
                        principalTable: "TrvTravelRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrvTravelRequestEmployees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravelRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrvTravelRequestEmployees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrvTravelRequestEmployees_TrvTravelRequests_TravelRequestId",
                        column: x => x.TravelRequestId,
                        principalTable: "TrvTravelRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelDocuments_TravelRequestId",
                table: "TrvTravelDocuments",
                column: "TravelRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequestEmployees_EmployeeId",
                table: "TrvTravelRequestEmployees",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequestEmployees_TravelRequestId",
                table: "TrvTravelRequestEmployees",
                column: "TravelRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_CreationTime",
                table: "TrvTravelRequests",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_Department",
                table: "TrvTravelRequests",
                column: "Department");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_RequesterId",
                table: "TrvTravelRequests",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_Status",
                table: "TrvTravelRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_Type",
                table: "TrvTravelRequests",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrvTravelDocuments");

            migrationBuilder.DropTable(
                name: "TrvTravelRequestEmployees");

            migrationBuilder.DropTable(
                name: "TrvTravelRequests");
        }
    }
}
