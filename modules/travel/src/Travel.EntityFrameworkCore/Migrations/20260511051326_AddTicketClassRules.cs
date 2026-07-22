using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketClassRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TicketClass",
                table: "TrvTravelRequestEmployees",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TicketClass",
                table: "TrvTravelFlightOffers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TicketClass",
                table: "TrvAllowanceRates",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequestEmployees_TicketClass",
                table: "TrvTravelRequestEmployees",
                column: "TicketClass");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelFlightOffers_TicketClass",
                table: "TrvTravelFlightOffers",
                column: "TicketClass");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRates_TicketClass",
                table: "TrvAllowanceRates",
                column: "TicketClass");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrvTravelRequestEmployees_TicketClass",
                table: "TrvTravelRequestEmployees");

            migrationBuilder.DropIndex(
                name: "IX_TrvTravelFlightOffers_TicketClass",
                table: "TrvTravelFlightOffers");

            migrationBuilder.DropIndex(
                name: "IX_TrvAllowanceRates_TicketClass",
                table: "TrvAllowanceRates");

            migrationBuilder.DropColumn(
                name: "TicketClass",
                table: "TrvTravelRequestEmployees");

            migrationBuilder.DropColumn(
                name: "TicketClass",
                table: "TrvTravelFlightOffers");

            migrationBuilder.DropColumn(
                name: "TicketClass",
                table: "TrvAllowanceRates");
        }
    }
}
