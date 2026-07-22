using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelFlightOffersSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SelectedDepartureFlightOfferId",
                table: "TrvTravelRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SelectedReturnFlightOfferId",
                table: "TrvTravelRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TrvTravelFlightOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravelRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Airline = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FlightNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DepartureTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArrivalTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Duration = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsSelected = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrvTravelFlightOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrvTravelFlightOffers_TrvTravelRequests_TravelRequestId",
                        column: x => x.TravelRequestId,
                        principalTable: "TrvTravelRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_SelectedDepartureFlightOfferId",
                table: "TrvTravelRequests",
                column: "SelectedDepartureFlightOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_SelectedReturnFlightOfferId",
                table: "TrvTravelRequests",
                column: "SelectedReturnFlightOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelFlightOffers_Direction",
                table: "TrvTravelFlightOffers",
                column: "Direction");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelFlightOffers_IsSelected",
                table: "TrvTravelFlightOffers",
                column: "IsSelected");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelFlightOffers_TravelRequestId",
                table: "TrvTravelFlightOffers",
                column: "TravelRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrvTravelFlightOffers");

            migrationBuilder.DropIndex(
                name: "IX_TrvTravelRequests_SelectedDepartureFlightOfferId",
                table: "TrvTravelRequests");

            migrationBuilder.DropIndex(
                name: "IX_TrvTravelRequests_SelectedReturnFlightOfferId",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "SelectedDepartureFlightOfferId",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "SelectedReturnFlightOfferId",
                table: "TrvTravelRequests");
        }
    }
}
