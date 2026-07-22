using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelRequestPrototypeDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowanceTiers",
                table: "TrvTravelRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "HasTicketCompensation",
                table: "TrvTravelRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsAwareness",
                table: "TrvTravelRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsTransportation",
                table: "TrvTravelRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UseHighestAllowance",
                table: "TrvTravelRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowanceTiers",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "HasTicketCompensation",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "NeedsAwareness",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "NeedsTransportation",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "UseHighestAllowance",
                table: "TrvTravelRequests");
        }
    }
}
