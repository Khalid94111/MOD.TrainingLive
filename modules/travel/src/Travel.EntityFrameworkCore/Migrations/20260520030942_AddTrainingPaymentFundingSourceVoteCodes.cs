using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingPaymentFundingSourceVoteCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClothingAllowanceFundingSourceVoteCode",
                table: "TrvTravelRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DailyAllowanceFundingSourceVoteCode",
                table: "TrvTravelRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HealthInsuranceFundingSourceVoteCode",
                table: "TrvTravelRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TicketFundingSourceVoteCode",
                table: "TrvTravelRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VisaFundingSourceVoteCode",
                table: "TrvTravelRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE [TrvTravelRequests]
                SET
                    [TicketFundingSourceVoteCode] = [FundingSourceVoteCode],
                    [VisaFundingSourceVoteCode] = [FundingSourceVoteCode],
                    [HealthInsuranceFundingSourceVoteCode] = [FundingSourceVoteCode],
                    [DailyAllowanceFundingSourceVoteCode] = [FundingSourceVoteCode],
                    [ClothingAllowanceFundingSourceVoteCode] = [FundingSourceVoteCode]
                WHERE [FundingSourceVoteCode] <> ''
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClothingAllowanceFundingSourceVoteCode",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "DailyAllowanceFundingSourceVoteCode",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "HealthInsuranceFundingSourceVoteCode",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "TicketFundingSourceVoteCode",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "VisaFundingSourceVoteCode",
                table: "TrvTravelRequests");
        }
    }
}
