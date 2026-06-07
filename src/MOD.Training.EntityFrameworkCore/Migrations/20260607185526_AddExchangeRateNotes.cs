using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class AddExchangeRateNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrnFinancialItems_TrnFinancialItems_ParentId",
                table: "TrnFinancialItems");

            migrationBuilder.DropIndex(
                name: "IX_TrnFinancialItems_ParentId",
                table: "TrnFinancialItems");

            migrationBuilder.DropIndex(
                name: "IX_TrnFinancialItems_TenantId_ParentId",
                table: "TrnFinancialItems");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "TrnExchangeRates",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrnFinancialItems_TenantId_NameAr",
                table: "TrnFinancialItems",
                columns: new[] { "TenantId", "NameAr" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrnFinancialItems_TenantId_NameAr",
                table: "TrnFinancialItems");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "TrnExchangeRates");

            migrationBuilder.CreateIndex(
                name: "IX_TrnFinancialItems_ParentId",
                table: "TrnFinancialItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_TrnFinancialItems_TenantId_ParentId",
                table: "TrnFinancialItems",
                columns: new[] { "TenantId", "ParentId" });

            migrationBuilder.AddForeignKey(
                name: "FK_TrnFinancialItems_TrnFinancialItems_ParentId",
                table: "TrnFinancialItems",
                column: "ParentId",
                principalTable: "TrnFinancialItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
