using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Travel.EntityFrameworkCore;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(TravelDbContext))]
    [Migration("20260524113700_AddBlobNamesToEmployeeDocuments")]
    public partial class AddBlobNamesToEmployeeDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TicketBlobName",
                table: "TrvTravelEmployeeDocuments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisaBlobName",
                table: "TrvTravelEmployeeDocuments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceBlobName",
                table: "TrvTravelEmployeeDocuments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TicketBlobName",
                table: "TrvTravelEmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "VisaBlobName",
                table: "TrvTravelEmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "InsuranceBlobName",
                table: "TrvTravelEmployeeDocuments");
        }
    }
}
