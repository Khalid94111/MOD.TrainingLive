using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class v4_8_0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppCasualCourseFinancialItemRanks_AppCasualCourseFinancials_CasualCourseFinancialId",
                table: "AppCasualCourseFinancialItemRanks");

            migrationBuilder.DropTable(
                name: "AppCasualCourseFinancials");

            migrationBuilder.RenameColumn(
                name: "FundingSource",
                table: "AppCasualCourses",
                newName: "FundingSourceName");

            migrationBuilder.RenameColumn(
                name: "CasualCourseFinancialId",
                table: "AppCasualCourseFinancialItemRanks",
                newName: "CasualCourseFinancialItemId");

            migrationBuilder.RenameIndex(
                name: "IX_AppCasualCourseFinancialItemRanks_TenantId_CasualCourseFinancialId_RankId",
                table: "AppCasualCourseFinancialItemRanks",
                newName: "IX_AppCasualCourseFinancialItemRanks_TenantId_CasualCourseFinancialItemId_RankId");

            migrationBuilder.RenameIndex(
                name: "IX_AppCasualCourseFinancialItemRanks_CasualCourseFinancialId",
                table: "AppCasualCourseFinancialItemRanks",
                newName: "IX_AppCasualCourseFinancialItemRanks_CasualCourseFinancialItemId");

            migrationBuilder.AddColumn<Guid>(
                name: "CountryId",
                table: "AppTrainingProviders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFromNebras",
                table: "AppTrainingProviders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NebrasId",
                table: "AppTrainingProviders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "AppTrainingProviders",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<Guid>(
                name: "SessionId",
                table: "AppPriceQuotes",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "CasualCourseId",
                table: "AppPriceQuotes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CityId",
                table: "AppPriceQuotes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CountryId",
                table: "AppPriceQuotes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSelected",
                table: "AppPriceQuotes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "QuotedPriceOMR",
                table: "AppPriceQuotes",
                type: "decimal(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualEndDate",
                table: "AppCasualCourses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualStartDate",
                table: "AppCasualCourses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CourseCost",
                table: "AppCasualCourses",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FundingSourceVoteCode",
                table: "AppCasualCourses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppCasualCourseFinancialItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CasualCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstimatedAmountOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    ActualAmountOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_AppCasualCourseFinancialItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppCasualCourseFinancialItems_AppCasualCourses_CasualCourseId",
                        column: x => x.CasualCourseId,
                        principalTable: "AppCasualCourses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppTravelInstructions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CasualCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartureDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArrivalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArrivalBackDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VisaRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    VisaNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InsuranceArranged = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    InsuranceProvider = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TicketsBooked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TicketReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CalculatedTravelDays = table.Column<int>(type: "int", nullable: false),
                    OverrideTravelDays = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
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
                    table.PrimaryKey("PK_AppTravelInstructions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HrGeographicalLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArabicName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EnglishName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LocationParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrGeographicalLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HrGeographicalLocations_HrGeographicalLocations_LocationParentId",
                        column: x => x.LocationParentId,
                        principalTable: "HrGeographicalLocations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppTrainingProviders_CountryId",
                table: "AppTrainingProviders",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTrainingProviders_IsFromNebras",
                table: "AppTrainingProviders",
                column: "IsFromNebras");

            migrationBuilder.CreateIndex(
                name: "IX_AppTrainingProviders_Scope",
                table: "AppTrainingProviders",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_AppPriceQuotes_CasualCourseId",
                table: "AppPriceQuotes",
                column: "CasualCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPriceQuotes_CityId",
                table: "AppPriceQuotes",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPriceQuotes_CountryId",
                table: "AppPriceQuotes",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCasualCourseFinancialItems_CasualCourseId_FinancialItemId",
                table: "AppCasualCourseFinancialItems",
                columns: new[] { "CasualCourseId", "FinancialItemId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppTravelInstructions_CasualCourseId",
                table: "AppTravelInstructions",
                column: "CasualCourseId",
                unique: true,
                filter: "[CasualCourseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppTravelInstructions_SessionId",
                table: "AppTravelInstructions",
                column: "SessionId",
                unique: true,
                filter: "[SessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HrGeographicalLocations_LocationParentId",
                table: "HrGeographicalLocations",
                column: "LocationParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppCasualCourseFinancialItemRanks_AppCasualCourseFinancialItems_CasualCourseFinancialItemId",
                table: "AppCasualCourseFinancialItemRanks",
                column: "CasualCourseFinancialItemId",
                principalTable: "AppCasualCourseFinancialItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppCasualCourseFinancialItemRanks_AppCasualCourseFinancialItems_CasualCourseFinancialItemId",
                table: "AppCasualCourseFinancialItemRanks");

            migrationBuilder.DropTable(
                name: "AppCasualCourseFinancialItems");

            migrationBuilder.DropTable(
                name: "AppTravelInstructions");

            migrationBuilder.DropTable(
                name: "HrGeographicalLocations");

            migrationBuilder.DropIndex(
                name: "IX_AppTrainingProviders_CountryId",
                table: "AppTrainingProviders");

            migrationBuilder.DropIndex(
                name: "IX_AppTrainingProviders_IsFromNebras",
                table: "AppTrainingProviders");

            migrationBuilder.DropIndex(
                name: "IX_AppTrainingProviders_Scope",
                table: "AppTrainingProviders");

            migrationBuilder.DropIndex(
                name: "IX_AppPriceQuotes_CasualCourseId",
                table: "AppPriceQuotes");

            migrationBuilder.DropIndex(
                name: "IX_AppPriceQuotes_CityId",
                table: "AppPriceQuotes");

            migrationBuilder.DropIndex(
                name: "IX_AppPriceQuotes_CountryId",
                table: "AppPriceQuotes");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "AppTrainingProviders");

            migrationBuilder.DropColumn(
                name: "IsFromNebras",
                table: "AppTrainingProviders");

            migrationBuilder.DropColumn(
                name: "NebrasId",
                table: "AppTrainingProviders");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "AppTrainingProviders");

            migrationBuilder.DropColumn(
                name: "CasualCourseId",
                table: "AppPriceQuotes");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "AppPriceQuotes");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "AppPriceQuotes");

            migrationBuilder.DropColumn(
                name: "IsSelected",
                table: "AppPriceQuotes");

            migrationBuilder.DropColumn(
                name: "QuotedPriceOMR",
                table: "AppPriceQuotes");

            migrationBuilder.DropColumn(
                name: "ActualEndDate",
                table: "AppCasualCourses");

            migrationBuilder.DropColumn(
                name: "ActualStartDate",
                table: "AppCasualCourses");

            migrationBuilder.DropColumn(
                name: "CourseCost",
                table: "AppCasualCourses");

            migrationBuilder.DropColumn(
                name: "FundingSourceVoteCode",
                table: "AppCasualCourses");

            migrationBuilder.RenameColumn(
                name: "FundingSourceName",
                table: "AppCasualCourses",
                newName: "FundingSource");

            migrationBuilder.RenameColumn(
                name: "CasualCourseFinancialItemId",
                table: "AppCasualCourseFinancialItemRanks",
                newName: "CasualCourseFinancialId");

            migrationBuilder.RenameIndex(
                name: "IX_AppCasualCourseFinancialItemRanks_TenantId_CasualCourseFinancialItemId_RankId",
                table: "AppCasualCourseFinancialItemRanks",
                newName: "IX_AppCasualCourseFinancialItemRanks_TenantId_CasualCourseFinancialId_RankId");

            migrationBuilder.RenameIndex(
                name: "IX_AppCasualCourseFinancialItemRanks_CasualCourseFinancialItemId",
                table: "AppCasualCourseFinancialItemRanks",
                newName: "IX_AppCasualCourseFinancialItemRanks_CasualCourseFinancialId");

            migrationBuilder.AlterColumn<Guid>(
                name: "SessionId",
                table: "AppPriceQuotes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "AppCasualCourseFinancials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActualAmountOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    CasualCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedAmountOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    FinancialItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Source = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppCasualCourseFinancials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppCasualCourseFinancials_AppCasualCourses_CasualCourseId",
                        column: x => x.CasualCourseId,
                        principalTable: "AppCasualCourses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppCasualCourseFinancials_CasualCourseId_FinancialItemId",
                table: "AppCasualCourseFinancials",
                columns: new[] { "CasualCourseId", "FinancialItemId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_AppCasualCourseFinancialItemRanks_AppCasualCourseFinancials_CasualCourseFinancialId",
                table: "AppCasualCourseFinancialItemRanks",
                column: "CasualCourseFinancialId",
                principalTable: "AppCasualCourseFinancials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
