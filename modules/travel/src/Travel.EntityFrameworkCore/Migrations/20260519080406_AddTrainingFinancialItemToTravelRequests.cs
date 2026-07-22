using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingFinancialItemToTravelRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceFinancialItem",
                table: "TrvTravelRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceFinancialItem",
                table: "TrvTravelRequests");
        }
    }
}
