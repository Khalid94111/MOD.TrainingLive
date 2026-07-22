using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel.Migrations
{
    /// <inheritdoc />
    public partial class AddFundingSourceVoteRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrvFundingSourceVoteRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FundingSourceVoteCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_TrvFundingSourceVoteRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrvFundingSourceVoteRules_IsActive",
                table: "TrvFundingSourceVoteRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TrvFundingSourceVoteRules_PaymentType",
                table: "TrvFundingSourceVoteRules",
                column: "PaymentType",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO [TrvFundingSourceVoteRules]
                    ([Id], [PaymentType], [FundingSourceVoteCode], [IsActive], [ExtraProperties], [ConcurrencyStamp], [CreationTime], [IsDeleted])
                VALUES
                    ('71000000-0000-0000-0000-000000000001', N'Ticket', N'TRV-TKT-001', 1, N'{}', NEWID(), GETDATE(), 0),
                    ('71000000-0000-0000-0000-000000000002', N'Visa', N'TRV-VSA-001', 1, N'{}', NEWID(), GETDATE(), 0),
                    ('71000000-0000-0000-0000-000000000003', N'HealthInsurance', N'TRV-HIN-001', 1, N'{}', NEWID(), GETDATE(), 0),
                    ('71000000-0000-0000-0000-000000000004', N'DailyAllowance', N'TRV-DAL-001', 1, N'{}', NEWID(), GETDATE(), 0),
                    ('71000000-0000-0000-0000-000000000005', N'ClothingAllowance', N'TRV-CLT-001', 1, N'{}', NEWID(), GETDATE(), 0)
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrvFundingSourceVoteRules");
        }
    }
}
