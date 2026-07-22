using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Travel.EntityFrameworkCore;

#nullable disable

namespace Travel.Migrations
{
    [DbContext(typeof(TravelDbContext))]
    [Migration("20260629000000_SwitchToTravelTypeDefinitionForeignKey")]
    public partial class SwitchToTravelTypeDefinitionForeignKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure seed data exists before adding FK constraints
            var creationTime = new DateTime(2026, 5, 6, 0, 0, 0, DateTimeKind.Utc);
            migrationBuilder.Sql($@"
                IF NOT EXISTS (SELECT 1 FROM TrvTravelTypeDefinitions WHERE Id = '11111111-1111-1111-1111-111111111101')
                INSERT INTO TrvTravelTypeDefinitions (Id, Code, Name, IsActive, ExtraProperties, ConcurrencyStamp, CreationTime, IsDeleted)
                VALUES
                    ('11111111-1111-1111-1111-111111111101', 1, N'دورة تدريبية', 1, '{{}}', '11111111111111111111111111111101', '{creationTime:yyyy-MM-dd}', 0),
                    ('11111111-1111-1111-1111-111111111102', 2, N'مهمة رسمية',   1, '{{}}', '11111111111111111111111111111102', '{creationTime:yyyy-MM-dd}', 0),
                    ('11111111-1111-1111-1111-111111111103', 3, N'علاج طبي',     1, '{{}}', '11111111111111111111111111111103', '{creationTime:yyyy-MM-dd}', 0),
                    ('11111111-1111-1111-1111-111111111104', 4, N'مؤتمر / ندوة', 1, '{{}}', '11111111111111111111111111111104', '{creationTime:yyyy-MM-dd}', 0),
                    ('11111111-1111-1111-1111-111111111105', 5, N'مسابقة',       1, '{{}}', '11111111111111111111111111111105', '{creationTime:yyyy-MM-dd}', 0)
            ");

            // Add TravelTypeDefinitionId to TravelRequests
            migrationBuilder.AddColumn<Guid>(
                name: "TravelTypeDefinitionId",
                table: "TrvTravelRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Back-fill: map old int Type values to seed GUIDs
            migrationBuilder.Sql(@"
                UPDATE TrvTravelRequests
                SET TravelTypeDefinitionId = CASE [Type]
                    WHEN 1 THEN '11111111-1111-1111-1111-111111111101'
                    WHEN 2 THEN '11111111-1111-1111-1111-111111111102'
                    WHEN 3 THEN '11111111-1111-1111-1111-111111111103'
                    WHEN 4 THEN '11111111-1111-1111-1111-111111111104'
                    WHEN 5 THEN '11111111-1111-1111-1111-111111111105'
                    ELSE '11111111-1111-1111-1111-111111111101'
                END
            ");

            // Drop old int Type column and its index
            migrationBuilder.DropIndex(
                name: "IX_TrvTravelRequests_Type",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "TrvTravelRequests");

            // Add FK index and constraint on TravelRequests
            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_TravelTypeDefinitionId",
                table: "TrvTravelRequests",
                column: "TravelTypeDefinitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrvTravelRequests_TrvTravelTypeDefinitions_TravelTypeDefinitionId",
                table: "TrvTravelRequests",
                column: "TravelTypeDefinitionId",
                principalTable: "TrvTravelTypeDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Add TravelTypeDefinitionId to AllowanceRules
            migrationBuilder.AddColumn<Guid>(
                name: "TravelTypeDefinitionId",
                table: "TrvAllowanceRules",
                type: "uniqueidentifier",
                nullable: true);

            // Back-fill AllowanceRules (TravelType was nullable int)
            migrationBuilder.Sql(@"
                UPDATE TrvAllowanceRules
                SET TravelTypeDefinitionId = CASE TravelType
                    WHEN 1 THEN '11111111-1111-1111-1111-111111111101'
                    WHEN 2 THEN '11111111-1111-1111-1111-111111111102'
                    WHEN 3 THEN '11111111-1111-1111-1111-111111111103'
                    WHEN 4 THEN '11111111-1111-1111-1111-111111111104'
                    WHEN 5 THEN '11111111-1111-1111-1111-111111111105'
                    ELSE NULL
                END
                WHERE TravelType IS NOT NULL
            ");

            // Drop old nullable TravelType column and its index from AllowanceRules
            migrationBuilder.DropIndex(
                name: "IX_TrvAllowanceRules_TravelType",
                table: "TrvAllowanceRules");

            migrationBuilder.DropColumn(
                name: "TravelType",
                table: "TrvAllowanceRules");

            // Add FK index and constraint on AllowanceRules
            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRules_TravelTypeDefinitionId",
                table: "TrvAllowanceRules",
                column: "TravelTypeDefinitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrvAllowanceRules_TrvTravelTypeDefinitions_TravelTypeDefinitionId",
                table: "TrvAllowanceRules",
                column: "TravelTypeDefinitionId",
                principalTable: "TrvTravelTypeDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove FK and index from AllowanceRules
            migrationBuilder.DropForeignKey(
                name: "FK_TrvAllowanceRules_TrvTravelTypeDefinitions_TravelTypeDefinitionId",
                table: "TrvAllowanceRules");

            migrationBuilder.DropIndex(
                name: "IX_TrvAllowanceRules_TravelTypeDefinitionId",
                table: "TrvAllowanceRules");

            migrationBuilder.DropColumn(
                name: "TravelTypeDefinitionId",
                table: "TrvAllowanceRules");

            // Restore old TravelType int column
            migrationBuilder.AddColumn<int>(
                name: "TravelType",
                table: "TrvAllowanceRules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRules_TravelType",
                table: "TrvAllowanceRules",
                column: "TravelType");

            // Remove FK and index from TravelRequests
            migrationBuilder.DropForeignKey(
                name: "FK_TrvTravelRequests_TrvTravelTypeDefinitions_TravelTypeDefinitionId",
                table: "TrvTravelRequests");

            migrationBuilder.DropIndex(
                name: "IX_TrvTravelRequests_TravelTypeDefinitionId",
                table: "TrvTravelRequests");

            migrationBuilder.DropColumn(
                name: "TravelTypeDefinitionId",
                table: "TrvTravelRequests");

            // Restore old Type int column
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "TrvTravelRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_Type",
                table: "TrvTravelRequests",
                column: "Type");
        }
    }
}
