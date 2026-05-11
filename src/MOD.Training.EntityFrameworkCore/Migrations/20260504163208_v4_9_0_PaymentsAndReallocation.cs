using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class v4_9_0_PaymentsAndReallocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppBudgetReallocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CasualCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CoursePaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FundingSourceVoteCode = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ToFinancialItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmountOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovalNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_AppBudgetReallocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppCoursePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CasualCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TrainingProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceAmountOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    NebrasAmountOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvoiceBlobName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InvoiceOriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_AppCoursePayments", x => x.Id);
                    table.CheckConstraint("CK_CoursePayment_PolymorphicParent", "([SessionId] IS NOT NULL AND [CasualCourseId] IS NULL) OR ([SessionId] IS NULL AND [CasualCourseId] IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "AppTravelAllowancePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CasualCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NominationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonnelType = table.Column<int>(type: "int", nullable: false),
                    TicketAmountOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    TravelAllowanceOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    ClothingAllowanceOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    InsuranceOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    VisaFeesOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    TotalOMR = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExternalRequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExternalStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExternalResponseAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_AppTravelAllowancePayments", x => x.Id);
                    table.CheckConstraint("CK_TravelAllowancePayment_PolymorphicParent", "([SessionId] IS NOT NULL AND [CasualCourseId] IS NULL) OR ([SessionId] IS NULL AND [CasualCourseId] IS NOT NULL)");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppBudgetReallocations_TenantId_CasualCourseId",
                table: "AppBudgetReallocations",
                columns: new[] { "TenantId", "CasualCourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppBudgetReallocations_TenantId_CoursePaymentId",
                table: "AppBudgetReallocations",
                columns: new[] { "TenantId", "CoursePaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppBudgetReallocations_TenantId_Status",
                table: "AppBudgetReallocations",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppCoursePayments_TenantId_CasualCourseId",
                table: "AppCoursePayments",
                columns: new[] { "TenantId", "CasualCourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppCoursePayments_TenantId_SessionId",
                table: "AppCoursePayments",
                columns: new[] { "TenantId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppCoursePayments_TenantId_Status",
                table: "AppCoursePayments",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppCoursePayments_TenantId_TrainingProviderId",
                table: "AppCoursePayments",
                columns: new[] { "TenantId", "TrainingProviderId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppTravelAllowancePayments_TenantId_CasualCourseId",
                table: "AppTravelAllowancePayments",
                columns: new[] { "TenantId", "CasualCourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppTravelAllowancePayments_TenantId_NominationId",
                table: "AppTravelAllowancePayments",
                columns: new[] { "TenantId", "NominationId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppTravelAllowancePayments_TenantId_SessionId",
                table: "AppTravelAllowancePayments",
                columns: new[] { "TenantId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppTravelAllowancePayments_TenantId_Status",
                table: "AppTravelAllowancePayments",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppBudgetReallocations");

            migrationBuilder.DropTable(
                name: "AppCoursePayments");

            migrationBuilder.DropTable(
                name: "AppTravelAllowancePayments");
        }
    }
}
