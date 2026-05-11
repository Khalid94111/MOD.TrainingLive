using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <summary>
    /// v4.8.0 — Phase 4B-α (Pre-Execution Preparation Layer).
    ///
    /// <para>Schema delta:</para>
    /// <list type="bullet">
    ///   <item><description>Creates <c>HrGeographicalLocations</c> (master HR data: countries + cities).</description></item>
    ///   <item><description>Extends <c>AppTrainingProviders</c> with Nebras sync fields (<c>IsFromNebras</c>, <c>NebrasId</c>),
    ///     a <c>Scope</c> enum (Internal=0 / Local=1 / International=2; default Local), and a soft FK <c>CountryId</c>.</description></item>
    ///   <item><description>Extends <c>AppCasualCourses</c> with nullable <c>ActualStartDate</c> + <c>ActualEndDate</c>
    ///     (written by <c>SelectPriceQuoteAsync</c>).</description></item>
    ///   <item><description>Extends <c>AppPriceQuotes</c>: makes <c>SessionId</c> nullable (polymorphic),
    ///     adds <c>CasualCourseId</c> (nullable), <c>QuotedPriceOMR</c>, <c>IsSelected</c> (default false),
    ///     and soft FKs <c>CountryId</c> + <c>CityId</c>. Adds CHECK constraint enforcing exactly one polymorphic parent.
    ///     Backfills <c>QuotedPriceOMR := QuotedPrice</c> for existing rows.</description></item>
    ///   <item><description>Creates <c>AppTravelInstructions</c> (one polymorphic instruction per casual course or session).
    ///     Adds CHECK constraint enforcing exactly one polymorphic parent. Adds unique-per-arm filtered indexes.</description></item>
    /// </list>
    ///
    /// <para>Cross-module FKs to <c>HrGeographicalLocations</c> are declared in EF config only (no DB-level constraints),
    /// matching the established <c>HrEmployees</c> / <c>HrRanks</c> reference pattern.</para>
    /// </summary>
    public partial class v4_8_0_PreExecutionLayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ───────────────────────────────────────────────────────────────
            // 1. HrGeographicalLocations — master HR reference (countries + cities).
            // ───────────────────────────────────────────────────────────────
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
                });

            migrationBuilder.CreateIndex(
                name: "IX_HrGeographicalLocations_LocationParentId",
                table: "HrGeographicalLocations",
                column: "LocationParentId");

            // ───────────────────────────────────────────────────────────────
            // 2. AppTrainingProviders extensions (Nebras + Scope + CountryId).
            // ───────────────────────────────────────────────────────────────
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
                defaultValue: 1); // ProviderScope.Local — safe default for existing rows.

            migrationBuilder.AddColumn<Guid>(
                name: "CountryId",
                table: "AppTrainingProviders",
                type: "uniqueidentifier",
                nullable: true);

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

            // ───────────────────────────────────────────────────────────────
            // 3. AppCasualCourses — actual confirmed dates (set at quote selection).
            // ───────────────────────────────────────────────────────────────
            migrationBuilder.AddColumn<DateTime>(
                name: "ActualStartDate",
                table: "AppCasualCourses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualEndDate",
                table: "AppCasualCourses",
                type: "datetime2",
                nullable: true);

            // ───────────────────────────────────────────────────────────────
            // 4. AppPriceQuotes — polymorphic activation + new columns.
            //    Drop existing FK + index on SessionId before altering column,
            //    then recreate as nullable.
            // ───────────────────────────────────────────────────────────────
            migrationBuilder.DropForeignKey(
                name: "FK_AppPriceQuotes_AppCourseSessions_SessionId",
                table: "AppPriceQuotes");

            migrationBuilder.DropIndex(
                name: "IX_AppPriceQuotes_SessionId",
                table: "AppPriceQuotes");

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

            migrationBuilder.AddColumn<decimal>(
                name: "QuotedPriceOMR",
                table: "AppPriceQuotes",
                type: "decimal(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsSelected",
                table: "AppPriceQuotes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "CountryId",
                table: "AppPriceQuotes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CityId",
                table: "AppPriceQuotes",
                type: "uniqueidentifier",
                nullable: true);

            // Backfill: legacy session-arm rows had only QuotedPrice.
            migrationBuilder.Sql(@"
                UPDATE [AppPriceQuotes]
                SET    [QuotedPriceOMR] = [QuotedPrice]
                WHERE  [QuotedPriceOMR] = 0
                  AND  [QuotedPrice] > 0;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_AppPriceQuotes_SessionId",
                table: "AppPriceQuotes",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPriceQuotes_CasualCourseId",
                table: "AppPriceQuotes",
                column: "CasualCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPriceQuotes_CountryId",
                table: "AppPriceQuotes",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPriceQuotes_CityId",
                table: "AppPriceQuotes",
                column: "CityId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppPriceQuotes_AppCourseSessions_SessionId",
                table: "AppPriceQuotes",
                column: "SessionId",
                principalTable: "AppCourseSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Polymorphic CHECK — exactly one parent (defense-in-depth alongside AppService validation).
            migrationBuilder.Sql(@"
                ALTER TABLE [AppPriceQuotes]
                ADD CONSTRAINT [CK_PriceQuote_PolymorphicParent]
                CHECK (
                    ([SessionId] IS NOT NULL AND [CasualCourseId] IS NULL) OR
                    ([SessionId] IS NULL AND [CasualCourseId] IS NOT NULL)
                );
            ");

            // ───────────────────────────────────────────────────────────────
            // 5. AppTravelInstructions — new polymorphic entity.
            // ───────────────────────────────────────────────────────────────
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
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}"),
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

            migrationBuilder.Sql(@"
                ALTER TABLE [AppTravelInstructions]
                ADD CONSTRAINT [CK_TravelInstruction_PolymorphicParent]
                CHECK (
                    ([CasualCourseId] IS NOT NULL AND [SessionId] IS NULL) OR
                    ([CasualCourseId] IS NULL AND [SessionId] IS NOT NULL)
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 5. Drop AppTravelInstructions.
            migrationBuilder.Sql("ALTER TABLE [AppTravelInstructions] DROP CONSTRAINT IF EXISTS [CK_TravelInstruction_PolymorphicParent];");
            migrationBuilder.DropTable(name: "AppTravelInstructions");

            // 4. Reverse AppPriceQuotes changes.
            migrationBuilder.Sql("ALTER TABLE [AppPriceQuotes] DROP CONSTRAINT IF EXISTS [CK_PriceQuote_PolymorphicParent];");

            migrationBuilder.DropForeignKey(
                name: "FK_AppPriceQuotes_AppCourseSessions_SessionId",
                table: "AppPriceQuotes");

            migrationBuilder.DropIndex(
                name: "IX_AppPriceQuotes_CityId",
                table: "AppPriceQuotes");
            migrationBuilder.DropIndex(
                name: "IX_AppPriceQuotes_CountryId",
                table: "AppPriceQuotes");
            migrationBuilder.DropIndex(
                name: "IX_AppPriceQuotes_CasualCourseId",
                table: "AppPriceQuotes");
            migrationBuilder.DropIndex(
                name: "IX_AppPriceQuotes_SessionId",
                table: "AppPriceQuotes");

            migrationBuilder.DropColumn(name: "CityId", table: "AppPriceQuotes");
            migrationBuilder.DropColumn(name: "CountryId", table: "AppPriceQuotes");
            migrationBuilder.DropColumn(name: "IsSelected", table: "AppPriceQuotes");
            migrationBuilder.DropColumn(name: "QuotedPriceOMR", table: "AppPriceQuotes");
            migrationBuilder.DropColumn(name: "CasualCourseId", table: "AppPriceQuotes");

            migrationBuilder.AlterColumn<Guid>(
                name: "SessionId",
                table: "AppPriceQuotes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppPriceQuotes_SessionId",
                table: "AppPriceQuotes",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppPriceQuotes_AppCourseSessions_SessionId",
                table: "AppPriceQuotes",
                column: "SessionId",
                principalTable: "AppCourseSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // 3. Reverse AppCasualCourses.
            migrationBuilder.DropColumn(name: "ActualEndDate", table: "AppCasualCourses");
            migrationBuilder.DropColumn(name: "ActualStartDate", table: "AppCasualCourses");

            // 2. Reverse AppTrainingProviders.
            migrationBuilder.DropIndex(name: "IX_AppTrainingProviders_Scope", table: "AppTrainingProviders");
            migrationBuilder.DropIndex(name: "IX_AppTrainingProviders_IsFromNebras", table: "AppTrainingProviders");
            migrationBuilder.DropIndex(name: "IX_AppTrainingProviders_CountryId", table: "AppTrainingProviders");

            migrationBuilder.DropColumn(name: "CountryId", table: "AppTrainingProviders");
            migrationBuilder.DropColumn(name: "Scope", table: "AppTrainingProviders");
            migrationBuilder.DropColumn(name: "NebrasId", table: "AppTrainingProviders");
            migrationBuilder.DropColumn(name: "IsFromNebras", table: "AppTrainingProviders");

            // 1. Drop HrGeographicalLocations.
            migrationBuilder.DropTable(name: "HrGeographicalLocations");
        }
    }
}
