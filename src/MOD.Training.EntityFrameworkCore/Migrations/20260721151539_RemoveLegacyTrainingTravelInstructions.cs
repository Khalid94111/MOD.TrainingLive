using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyTrainingTravelInstructions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve access for users/roles that had the equivalent legacy permissions.
            migrationBuilder.Sql(
                """
                UPDATE legacyGrant
                SET [Name] = 'TrainingExecution.TravelRequests.Send'
                FROM [AbpPermissionGrants] AS legacyGrant
                WHERE legacyGrant.[Name] = 'TrainingExecution.TravelInstructions.Issue'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [AbpPermissionGrants] AS currentGrant
                      WHERE currentGrant.[Name] = 'TrainingExecution.TravelRequests.Send'
                        AND currentGrant.[ProviderName] = legacyGrant.[ProviderName]
                        AND currentGrant.[ProviderKey] = legacyGrant.[ProviderKey]
                        AND (currentGrant.[TenantId] = legacyGrant.[TenantId]
                             OR (currentGrant.[TenantId] IS NULL AND legacyGrant.[TenantId] IS NULL)));

                DELETE FROM [AbpPermissionGrants]
                WHERE [Name] = 'TrainingExecution.TravelInstructions.Issue';

                UPDATE legacyGrant
                SET [Name] = 'TrainingExecution.TravelRequests'
                FROM [AbpPermissionGrants] AS legacyGrant
                WHERE legacyGrant.[Name] = 'TrainingExecution.TravelInstructions'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [AbpPermissionGrants] AS currentGrant
                      WHERE currentGrant.[Name] = 'TrainingExecution.TravelRequests'
                        AND currentGrant.[ProviderName] = legacyGrant.[ProviderName]
                        AND currentGrant.[ProviderKey] = legacyGrant.[ProviderKey]
                        AND (currentGrant.[TenantId] = legacyGrant.[TenantId]
                             OR (currentGrant.[TenantId] IS NULL AND legacyGrant.[TenantId] IS NULL)));

                DELETE FROM [AbpPermissionGrants]
                WHERE [Name] IN (
                    'TrainingExecution.TravelInstructions',
                    'TrainingExecution.TravelInstructions.Edit',
                    'TrainingExecution.TravelInstructions.Cancel',
                    'TrainingPayments.TravelAllowance.Create',
                    'TrainingPayments.TravelAllowance.Update',
                    'TrainingPayments.TravelAllowance.Delete',
                    'TrainingPayments.TravelAllowance.Confirm');

                -- Rows without an external request were created by Training's retired manual flow.
                DELETE FROM [AppTravelAllowancePayments]
                WHERE [ExternalRequestId] IS NULL
                   OR LTRIM(RTRIM([ExternalRequestId])) = '';
                """);

            migrationBuilder.DropTable(
                name: "AppTravelInstructions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppTravelInstructions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArrivalBackDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArrivalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CalculatedTravelDays = table.Column<int>(type: "int", nullable: false),
                    CasualCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DepartureDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InsuranceArranged = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    InsuranceProvider = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OverrideTravelDays = table.Column<int>(type: "int", nullable: true),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TicketReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TicketsBooked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    VisaNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VisaRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
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
        }
    }
}
