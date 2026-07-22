using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class MoveTravelRequestCategoryToRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "TrvTravelRequests",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_Category",
                table: "TrvTravelRequests",
                column: "Category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrvTravelRequests_Category",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "TrvTravelRequests");
        }
    }
}
