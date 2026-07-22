using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class RenameFinancialItemToFundingSourceVoteCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SourceFinancialItem",
                table: "TrvTravelRequests",
                newName: "FundingSourceVoteCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FundingSourceVoteCode",
                table: "TrvTravelRequests",
                newName: "SourceFinancialItem");
        }
    }
}
