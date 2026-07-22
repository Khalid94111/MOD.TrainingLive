using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class TravelTypesTableAndRuleScopeCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrvAllowanceRules_RankId",
                table: "TrvAllowanceRules");

            migrationBuilder.DropColumn(
                name: "RankId",
                table: "TrvAllowanceRules");

            migrationBuilder.CreateTable(
                name: "TrvTravelTypeDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrvTravelTypeDefinitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelTypeDefinitions_Code",
                table: "TrvTravelTypeDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelTypeDefinitions_IsActive",
                table: "TrvTravelTypeDefinitions",
                column: "IsActive");

            var creationTime = new DateTime(2026, 5, 6, 0, 0, 0, DateTimeKind.Utc);
            migrationBuilder.InsertData(
                table: "TrvTravelTypeDefinitions",
                columns: new[]
                {
                    "Id", "Code", "Name", "IsActive", "ExtraProperties", "ConcurrencyStamp",
                    "CreationTime", "CreatorId", "LastModificationTime", "LastModifierId",
                    "IsDeleted", "DeleterId", "DeletionTime"
                },
                values: new object[,]
                {
                    { Guid.Parse("11111111-1111-1111-1111-111111111101"), 1, "دورة تدريبية", true, "{}", "11111111111111111111111111111101", creationTime, null, null, null, false, null, null },
                    { Guid.Parse("11111111-1111-1111-1111-111111111102"), 2, "مهمة رسمية", true, "{}", "11111111111111111111111111111102", creationTime, null, null, null, false, null, null },
                    { Guid.Parse("11111111-1111-1111-1111-111111111103"), 3, "علاج طبي", true, "{}", "11111111111111111111111111111103", creationTime, null, null, null, false, null, null },
                    { Guid.Parse("11111111-1111-1111-1111-111111111104"), 4, "مؤتمر / ندوة", true, "{}", "11111111111111111111111111111104", creationTime, null, null, null, false, null, null },
                    { Guid.Parse("11111111-1111-1111-1111-111111111105"), 5, "مسابقة", true, "{}", "11111111111111111111111111111105", creationTime, null, null, null, false, null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrvTravelTypeDefinitions");

            migrationBuilder.AddColumn<Guid>(
                name: "RankId",
                table: "TrvAllowanceRules",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRules_RankId",
                table: "TrvAllowanceRules",
                column: "RankId");
        }
    }
}
