using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Travel.EntityFrameworkCore;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(TravelDbContext))]
    [Migration("20260518110000_AddTrainingSourceTenantToTravelRequests")]
    public partial class AddTrainingSourceTenantToTravelRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceTenantId",
                table: "TrvTravelRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_SourceTenantId",
                table: "TrvTravelRequests",
                column: "SourceTenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrvTravelRequests_SourceTenantId",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "SourceTenantId",
                table: "TrvTravelRequests");
        }
    }
}
