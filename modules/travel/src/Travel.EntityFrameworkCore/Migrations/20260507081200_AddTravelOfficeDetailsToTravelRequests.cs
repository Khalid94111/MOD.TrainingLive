using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelOfficeDetailsToTravelRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresVisa",
                table: "TrvTravelRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "VisaCostPerEmployee",
                table: "TrvTravelRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresTravelInsurance",
                table: "TrvTravelRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TravelInsuranceCostPerEmployee",
                table: "TrvTravelRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TicketAirline",
                table: "TrvTravelRequests",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TicketFlightNumber",
                table: "TrvTravelRequests",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "TicketDepartureTime",
                table: "TrvTravelRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TicketArrivalTime",
                table: "TrvTravelRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TicketCostPerEmployee",
                table: "TrvTravelRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TravelOfficeNotes",
                table: "TrvTravelRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "RequiresVisa", table: "TrvTravelRequests");
            migrationBuilder.DropColumn(name: "VisaCostPerEmployee", table: "TrvTravelRequests");
            migrationBuilder.DropColumn(name: "RequiresTravelInsurance", table: "TrvTravelRequests");
            migrationBuilder.DropColumn(name: "TravelInsuranceCostPerEmployee", table: "TrvTravelRequests");
            migrationBuilder.DropColumn(name: "TicketAirline", table: "TrvTravelRequests");
            migrationBuilder.DropColumn(name: "TicketFlightNumber", table: "TrvTravelRequests");
            migrationBuilder.DropColumn(name: "TicketDepartureTime", table: "TrvTravelRequests");
            migrationBuilder.DropColumn(name: "TicketArrivalTime", table: "TrvTravelRequests");
            migrationBuilder.DropColumn(name: "TicketCostPerEmployee", table: "TrvTravelRequests");
            migrationBuilder.DropColumn(name: "TravelOfficeNotes", table: "TrvTravelRequests");
        }
    }
}
