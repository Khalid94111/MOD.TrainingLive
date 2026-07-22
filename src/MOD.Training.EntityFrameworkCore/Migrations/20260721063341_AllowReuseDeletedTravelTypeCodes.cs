using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class AllowReuseDeletedTravelTypeCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrvTravelTypeDefinitions_Code",
                table: "TrvTravelTypeDefinitions");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelTypeDefinitions_Code",
                table: "TrvTravelTypeDefinitions",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrvTravelTypeDefinitions_Code",
                table: "TrvTravelTypeDefinitions");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelTypeDefinitions_Code",
                table: "TrvTravelTypeDefinitions",
                column: "Code",
                unique: true);
        }
    }
}
