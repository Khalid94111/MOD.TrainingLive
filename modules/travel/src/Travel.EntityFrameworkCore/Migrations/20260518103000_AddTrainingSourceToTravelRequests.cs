using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Travel.EntityFrameworkCore;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(TravelDbContext))]
    [Migration("20260518103000_AddTrainingSourceToTravelRequests")]
    public partial class AddTrainingSourceToTravelRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IntegrationWarnings",
                table: "TrvTravelRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SourceSystem",
                table: "TrvTravelRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceTrainingCourseId",
                table: "TrvTravelRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceTrainingCourseName",
                table: "TrvTravelRequests",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_SourceTrainingCourseId",
                table: "TrvTravelRequests",
                column: "SourceTrainingCourseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrvTravelRequests_SourceTrainingCourseId",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "IntegrationWarnings",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "SourceSystem",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "SourceTrainingCourseId",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "SourceTrainingCourseName",
                table: "TrvTravelRequests");
        }
    }
}
