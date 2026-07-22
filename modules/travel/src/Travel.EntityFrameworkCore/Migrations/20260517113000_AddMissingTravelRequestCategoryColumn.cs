using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Travel.EntityFrameworkCore;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(TravelDbContext))]
    [Migration("20260517113000_AddMissingTravelRequestCategoryColumn")]
    public partial class AddMissingTravelRequestCategoryColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('TrvTravelRequests', 'Category') IS NULL
                BEGIN
                    ALTER TABLE [TrvTravelRequests]
                    ADD [Category] int NOT NULL
                        CONSTRAINT [DF_TrvTravelRequests_Category_MigrationFix] DEFAULT 1;

                    ALTER TABLE [TrvTravelRequests]
                    DROP CONSTRAINT [DF_TrvTravelRequests_Category_MigrationFix];
                END

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_TrvTravelRequests_Category'
                      AND [object_id] = OBJECT_ID(N'[TrvTravelRequests]')
                )
                BEGIN
                    CREATE INDEX [IX_TrvTravelRequests_Category]
                    ON [TrvTravelRequests] ([Category]);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_TrvTravelRequests_Category'
                      AND [object_id] = OBJECT_ID(N'[TrvTravelRequests]')
                )
                BEGIN
                    DROP INDEX [IX_TrvTravelRequests_Category]
                    ON [TrvTravelRequests];
                END

                IF COL_LENGTH('TrvTravelRequests', 'Category') IS NOT NULL
                BEGIN
                    ALTER TABLE [TrvTravelRequests]
                    DROP COLUMN [Category];
                END
                """);
        }
    }
}
