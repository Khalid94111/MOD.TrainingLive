using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelEmployeeDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrvTravelEmployeeDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravelRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TicketNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Pnr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TicketFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    VisaFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    InsuranceFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrvTravelEmployeeDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrvTravelEmployeeDocuments_TrvTravelRequests_TravelRequestId",
                        column: x => x.TravelRequestId,
                        principalTable: "TrvTravelRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelEmployeeDocuments_EmployeeId",
                table: "TrvTravelEmployeeDocuments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelEmployeeDocuments_TravelRequestId",
                table: "TrvTravelEmployeeDocuments",
                column: "TravelRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrvTravelEmployeeDocuments");
        }
    }
}
